using GM.CatalogSync.Application.Services;
using GM.CatalogSync.Domain.Interfaces;
using GM.CatalogSync.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;
using Shared.Infrastructure;
using Shared.Infrastructure.Logging;
using Shared.Infrastructure.Services;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ⚙️ Configurar URLs fijas SOLO en desarrollo
// En producción, IIS maneja el puerto automáticamente según el binding del sitio
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("https://localhost:5001", "http://localhost:5000");
}
// En producción, IIS maneja el puerto - no configurar UseUrls()

// ⚙️ Configurar Serilog con hora de México
// El enricher agrega MexicoTime con la hora de México, usamos esa propiedad en el outputTemplate
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
        Title = "GM CatalogSync API",
        Version = "v1",
        Description = "API para sincronización de catálogos GM"
    });

    // Incluir comentarios XML en la documentación (API y Application)
    var xmlFileApi = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPathApi = Path.Combine(AppContext.BaseDirectory, xmlFileApi);
    if (File.Exists(xmlPathApi))
    {
        c.IncludeXmlComments(xmlPathApi);
    }
    
    // Incluir XML de Application (donde están los DTOs)
    var xmlFileApp = "GM.CatalogSync.Application.xml";
    var xmlPathApp = Path.Combine(AppContext.BaseDirectory, xmlFileApp);
    if (File.Exists(xmlPathApp))
    {
        c.IncludeXmlComments(xmlPathApp);
    }

    // Agrupar endpoints por recurso (extraer del path)
    c.TagActionsBy(api =>
    {
        var path = api.RelativePath ?? "";
        // Extraer el nombre del recurso del path: /api/v1/gm/catalog-sync/{recurso}
        var segments = path.Split('/');
        if (segments.Length >= 5)
        {
            var resourcePath = segments[4];
            
            // Caso especial: agrupar todos los endpoints de "product-list" bajo "ProductList"
            if (resourcePath.StartsWith("product-list"))
            {
                return new[] { "ProductList" };
            }
            
            // Convertir kebab-case a PascalCase: "carga-archivos-sinc" -> "CargaArchivosSinc"
            var resource = resourcePath.Split('-')
                .Select(s => char.ToUpper(s[0]) + s.Substring(1).ToLower())
                .Aggregate((a, b) => a + b);
            return new[] { resource };
        }
        return new[] { api.ActionDescriptor.RouteValues["controller"] };
    });

    // Ordenar operaciones por método HTTP: GET, POST, PUT, PATCH, DELETE
    // Los métodos del mismo tipo deben ir juntos (ej: todos los GET primero)
    c.OrderActionsBy(apiDesc =>
    {
        var methodOrder = apiDesc.HttpMethod?.ToUpper() switch
        {
            "GET" => "0",
            "POST" => "1",
            "PUT" => "2",
            "PATCH" => "3",
            "DELETE" => "4",
            _ => "9"
        };
        
        // Ordenar primero por método HTTP, luego por longitud del path (rutas cortas primero)
        var path = apiDesc.RelativePath ?? "";
        var pathLength = path.Length.ToString("D4");
        
        return $"{methodOrder}_{pathLength}_{path}";
    });

    // Configurar JWT en Swagger (igual que OracleAPI que funciona)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. \r\n\r\n Ingrese 'Bearer' [espacio] y luego su token en el cuadro de texto de abajo.\r\n\r\nEjemplo: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

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

// ⚙️ Configurar Oracle Connection Factory
var connectionString = builder.Configuration.GetConnectionString("Oracle");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'Oracle' no encontrada en appsettings.json");
}

builder.Services.AddScoped<IOracleConnectionFactory>(sp =>
    new OracleConnectionFactory(connectionString, sp.GetService<ILogger<OracleConnectionFactory>>()));

// ⚙️ Configurar Dependency Injection
// Productos
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();

// Carga de Archivos de Sincronización
builder.Services.AddScoped<ICargaArchivoSincRepository, CargaArchivoSincRepository>();
builder.Services.AddScoped<ICargaArchivoSincService, CargaArchivoSincService>();

// Sincronización de Archivos por Dealer
builder.Services.AddScoped<ISincArchivoDealerRepository, SincArchivoDealerRepository>();
builder.Services.AddScoped<ISincArchivoDealerService, SincArchivoDealerService>();
builder.Services.AddScoped<IDistribuidorRepository, DistribuidorRepository>();

// Foto de Dealers Carga Archivos Sincronización
builder.Services.AddScoped<IFotoDealersCargaArchivosSincRepository, FotoDealersCargaArchivosSincRepository>();
builder.Services.AddScoped<IFotoDealersCargaArchivosSincService, FotoDealersCargaArchivosSincService>();

// ⚙️ Registrar servicios en segundo plano para monitoreo y mantenimiento
// PerformanceMonitor: Mantiene la aplicación activa y monitorea rendimiento
builder.Services.AddSingleton<PerformanceMonitor>();
builder.Services.AddHostedService<PerformanceMonitor>(provider => 
    provider.GetRequiredService<PerformanceMonitor>());

// ConnectionPoolMaintenance: Mantiene conexiones Oracle activas (warm-up periódico)
builder.Services.AddSingleton<ConnectionPoolMaintenance>();
builder.Services.AddHostedService<ConnectionPoolMaintenance>(provider => 
    provider.GetRequiredService<ConnectionPoolMaintenance>());

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
// Cuando la app se ejecuta bajo una ruta como /GM.CatalogSync.API
// IIS envía el header X-Forwarded-PathBase o se puede configurar manualmente
var pathBase = app.Configuration["PathBase"];
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
    Log.Information("🔧 PathBase configurado: {PathBase}", pathBase);
}

// ⚙️ Configurar pipeline
// ⚙️ Swagger y Scalar habilitados en todos los entornos (API interna)
// Para APIs públicas, mantener solo en Development
app.UseSwagger();

// Configurar Swagger UI
app.UseSwaggerUI(c =>
{
    // Usar ruta relativa para que funcione en sub-aplicaciones IIS
    c.SwaggerEndpoint("v1/swagger.json", "GM CatalogSync API v1");
    c.RoutePrefix = "swagger"; // Swagger en /swagger
    
    // Configurar persistencia del token en Swagger UI
    c.ConfigObject.PersistAuthorization = true;
    
    // Configurar ordenamiento de operaciones por método HTTP (GET, POST, PUT, etc.)
    c.ConfigObject.AdditionalItems["operationsSorter"] = "method";
    c.ConfigObject.AdditionalItems["tagsSorter"] = "alpha";
});

// Configurar Scalar
// Scalar detecta automáticamente el documento Swagger generado por Swashbuckle
// La ruta estándar de Swashbuckle es /swagger/{documentName}/swagger.json
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("GM CatalogSync API")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithTheme(ScalarTheme.BluePlanet);
});

app.UseCors();

// ⚙️ Configurar HTTPS Redirection
// En desarrollo: deshabilitado para evitar perder headers en redirects
// En producción: habilitado para forzar HTTPS
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

Log.Information("🚀 GM CatalogSync API iniciada");

app.Run();

// ⚙️ Clase para configuración JWT
public class JwtConfig
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
}
