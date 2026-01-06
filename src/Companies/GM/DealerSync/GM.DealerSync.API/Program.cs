using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;
using Shared.Infrastructure;
using Shared.Infrastructure.Logging;
using Shared.Infrastructure.Services;
using StackExchange.Redis;
// RedLock ya no se usa - ahora usamos Redis directo
// using RedLockNet;
// using RedLockNet.SERedis;
// using RedLockNet.SERedis.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ⚙️ Configurar URLs fijas SOLO en desarrollo
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://localhost:5000");
}

// ⚙️ Configurar Serilog con hora de México
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.With(new MexicoTimeEnricher())
    .WriteTo.Console(outputTemplate: "[{MexicoTime:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("Logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{MexicoTime:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// ⚙️ Configurar servicios
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ⚙️ Configurar Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "GM DealerSync API",
        Version = "v1",
        Description = "API para recepción de webhooks y sincronización de catálogos desde sistema central"
    });

    // Incluir comentarios XML en la documentación (API y Application)
    var xmlFileApi = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPathApi = Path.Combine(AppContext.BaseDirectory, xmlFileApi);
    if (File.Exists(xmlPathApi))
    {
        c.IncludeXmlComments(xmlPathApi);
    }

    // Incluir XML de Application (donde están los DTOs)
    var xmlFileApp = "GM.DealerSync.Application.xml";
    var xmlPathApp = Path.Combine(AppContext.BaseDirectory, xmlFileApp);
    if (File.Exists(xmlPathApp))
    {
        c.IncludeXmlComments(xmlPathApp);
    }

    // Agrupar endpoints por recurso
    c.TagActionsBy(api =>
    {
        var path = api.RelativePath ?? "";
        var segments = path.Split('/');
        if (segments.Length >= 5)
        {
            var resourcePath = segments[4];
            var resource = resourcePath.Split('-')
                .Select(s => char.ToUpper(s[0]) + s.Substring(1).ToLower())
                .Aggregate((a, b) => a + b);
            return new[] { resource };
        }
        return new[] { api.ActionDescriptor.RouteValues["controller"] };
    });

    // Ordenar operaciones por método HTTP
    c.OrderActionsBy(apiDesc =>
    {
        var methodOrder = apiDesc.HttpMethod?.ToUpper() switch
        {
            "GET" => 1,
            "POST" => 2,
            "PUT" => 3,
            "PATCH" => 4,
            "DELETE" => 5,
            _ => 99
        };
        return $"{methodOrder:D2}_{apiDesc.RelativePath}";
    });

    // Configurar seguridad JWT (si se usa)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ⚙️ Configurar Redis Connection
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
IConnectionMultiplexer? redisConnection = null;

if (string.IsNullOrWhiteSpace(redisConnectionString))
{
    Log.Warning("⚠️ Redis connection string no encontrada. RedLock no estará disponible.");
}
else
{
    try
    {
        redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
        builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
        Log.Information("✅ Redis conectado exitosamente");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "⚠️ Error al conectar con Redis. La aplicación continuará sin RedLock. " +
                       "Para habilitar distributed locking, asegúrese de que Redis esté disponible.");
        // No lanzar excepción - permitir que la app inicie sin Redis
    }
}

// ⚙️ RedLock deshabilitado - Ahora usamos Redis directo (más simple y predecible)
// Ya no necesitamos RedLockFactory porque usamos IConnectionMultiplexer directamente
Log.Information("✅ Usando Redis directo (sin RedLock) para distributed locking");

// ⚙️ Hangfire deshabilitado - Usando Task.Run directamente para evitar dependencia de SQL Server
// Los jobs se ejecutan en background usando Task.Run con manejo correcto de logs

// ⚙️ Configurar JWT
var jwtConfig = builder.Configuration.GetSection("Jwt").Get<JwtConfig>();
if (jwtConfig == null || string.IsNullOrWhiteSpace(jwtConfig.Key))
{
    throw new InvalidOperationException("La configuración JWT no está completa en appsettings.json. Se requiere 'Jwt:Key'.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Key)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5) // Tolerancia de 5 minutos para diferencias de reloj
        };
        
        // Agregar eventos para logging de errores de autenticación
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var exceptionType = context.Exception.GetType().Name;
                var exceptionMessage = context.Exception.Message;
                var innerException = context.Exception.InnerException?.Message;
                
                // Obtener el token del request para debug
                var token = context.Request.Headers["Authorization"].ToString();
                var tokenPreview = string.IsNullOrEmpty(token) 
                    ? "NO TOKEN" 
                    : (token.Length > 50 ? token.Substring(0, 50) + "..." : token);
                
                Log.Warning(
                    "JWT Authentication failed. Path: {Path}, Type: {Type}, Error: {Error}, Inner: {Inner}, TokenPreview: {TokenPreview}", 
                    context.Request.Path, exceptionType, exceptionMessage, innerException ?? "None", tokenPreview);
                
                // Log adicional para problemas comunes
                if (context.Exception is SecurityTokenExpiredException)
                {
                    Log.Warning("Token has expired. Path: {Path}", context.Request.Path);
                }
                else if (context.Exception is SecurityTokenInvalidSignatureException)
                {
                    Log.Warning("Token signature is invalid - Key mismatch possible. Path: {Path}, ExpectedKeyLength: {KeyLength} chars", 
                        context.Request.Path, jwtConfig.Key.Length);
                }
                else if (context.Exception is SecurityTokenInvalidIssuerException)
                {
                    Log.Warning("Token issuer is invalid. Path: {Path}", context.Request.Path);
                }
                else if (context.Exception is SecurityTokenInvalidAudienceException)
                {
                    Log.Warning("Token audience is invalid. Path: {Path}", context.Request.Path);
                }
                else if (context.Exception is SecurityTokenMalformedException)
                {
                    Log.Warning("Token is malformed. Path: {Path}, TokenPreview: {TokenPreview}", 
                        context.Request.Path, tokenPreview);
                }
                
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var userId = context.Principal?.FindFirst("USUARIO")?.Value ?? 
                            context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                            context.Principal?.Identity?.Name ?? "Unknown";
                Log.Information("JWT Token validated successfully. Path: {Path}, User: {User}", 
                    context.Request.Path, userId);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var token = context.Request.Headers["Authorization"].ToString();
                var hasToken = !string.IsNullOrEmpty(token);
                
                Log.Warning(
                    "JWT Challenge triggered. Path: {Path}, Error: {Error}, Description: {Description}, HasToken: {HasToken}", 
                    context.Request.Path, context.Error, context.ErrorDescription, hasToken);
                return Task.CompletedTask;
            },
            OnForbidden = context =>
            {
                Log.Warning("JWT Forbidden - User authenticated but lacks permission. Path: {Path}", 
                    context.Request.Path);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
Log.Information("✅ JWT configurado exitosamente");

// ⚙️ Configurar Oracle Connection Factory (opcional para pruebas)
var oracleConnectionString = builder.Configuration.GetConnectionString("Oracle");
if (string.IsNullOrWhiteSpace(oracleConnectionString))
{
    Log.Warning("⚠️ Connection string 'Oracle' no encontrada. Algunas funcionalidades pueden no estar disponibles.");
}
else
{
    builder.Services.AddScoped<IOracleConnectionFactory>(sp =>
        new OracleConnectionFactory(oracleConnectionString, sp.GetService<ILogger<OracleConnectionFactory>>()));
    Log.Information("✅ Oracle Connection Factory configurado");
}

// ⚙️ Configurar Dependency Injection - Servicios de sincronización
// Registrar DistributedLockService (siempre, pero IConnectionMultiplexer puede ser null si Redis no está disponible)
builder.Services.AddScoped<GM.DealerSync.Domain.Interfaces.IDistributedLockService>(sp =>
{
    var redis = sp.GetService<IConnectionMultiplexer>();
    var logger = sp.GetRequiredService<ILogger<GM.DealerSync.Application.Services.DistributedLockService>>();
    return new GM.DealerSync.Application.Services.DistributedLockService(redis, logger);
});
Log.Information("✅ DistributedLockService registrado (usando Redis directo)");

builder.Services.AddScoped<GM.DealerSync.Application.Services.IBatchSyncJobService, GM.DealerSync.Application.Services.BatchSyncJobService>();
Log.Information("✅ BatchSyncJobService registrado");

builder.Services.AddSingleton<GM.DealerSync.Application.Services.IProcessTypeService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<GM.DealerSync.Application.Services.ProcessTypeService>>();
    return new GM.DealerSync.Application.Services.ProcessTypeService(configuration, logger);
});
Log.Information("✅ ProcessTypeService registrado (leyendo desde appsettings.json)");

// ⚙️ Registrar servicios en segundo plano para monitoreo y mantenimiento
// PerformanceMonitor: Mantiene la aplicación activa y monitorea rendimiento
builder.Services.AddSingleton<PerformanceMonitor>();
builder.Services.AddHostedService<PerformanceMonitor>(provider => 
    provider.GetRequiredService<PerformanceMonitor>());
Log.Information("✅ PerformanceMonitor registrado");

// ConnectionPoolMaintenance: Mantiene conexiones Oracle activas (warm-up periódico)
builder.Services.AddSingleton<ConnectionPoolMaintenance>();
builder.Services.AddHostedService<ConnectionPoolMaintenance>(provider => 
    provider.GetRequiredService<ConnectionPoolMaintenance>());
Log.Information("✅ ConnectionPoolMaintenance registrado");

// ⚙️ Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ⚙️ Configurar PathBase para sub-aplicaciones en IIS
var pathBase = app.Configuration["PathBase"];
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
    Log.Information("🔧 PathBase configurado: {PathBase}", pathBase);
}

// ⚙️ Configurar pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "GM DealerSync API v1");
    c.RoutePrefix = "swagger";
    c.ConfigObject.PersistAuthorization = true;
    c.ConfigObject.AdditionalItems["operationsSorter"] = "method";
    c.ConfigObject.AdditionalItems["tagsSorter"] = "alpha";
});

// Configurar Scalar
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("GM DealerSync API")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithTheme(ScalarTheme.BluePlanet);
});

// ⚙️ Hangfire Dashboard deshabilitado - No se requiere SQL Server

app.UseCors();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Middleware temporal para debug de autenticación (después de UseAuthentication)
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader))
        {
            Log.Warning("Request to {Path} without Authorization header. Method: {Method}", 
                context.Request.Path, context.Request.Method);
        }
        else
        {
            var tokenPreview = authHeader.Length > 50 ? authHeader.Substring(0, 50) + "..." : authHeader;
            Log.Information("Request to {Path} with Authorization header. Method: {Method}, TokenPreview: {TokenPreview}", 
                context.Request.Path, context.Request.Method, tokenPreview);
        }
    }
    await next();
});

app.MapControllers();

Log.Information("🚀 GM DealerSync API iniciada");
app.Run();

// ⚙️ Clases de configuración
public class JwtConfig
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
}

// ⚙️ HangfireAuthorizationFilter removido - Se agregará cuando se configure Hangfire
