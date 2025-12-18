# 📦 Plantilla para Crear Nuevos Módulos

Este documento te guía paso a paso para crear un nuevo módulo siguiendo la arquitectura de GlobalOracleAPI.

---

## 🎯 Pasos para Crear un Nuevo Módulo

### Paso 1: Identificar el Módulo

**Preguntas clave:**
- ¿Es específico de una empresa? → `Companies/{Company}/{Module}/`
- ¿Es compartido por 2+ empresas? → `Domains/{Module}/`
- ¿Tendrá 10-100 endpoints? → Sí, crear módulo
- ¿Tendrá >100 endpoints? → Dividir en submódulos

### Paso 2: Crear la Estructura de Carpetas

**Ejemplo: Crear `GM.Sales`**

```bash
src/Companies/GM/Sales/
├── GM.Sales.API/
├── GM.Sales.Application/
├── GM.Sales.Domain/
└── GM.Sales.Infrastructure/
```

### Paso 3: Crear los Proyectos

#### 3.1. Crear GM.Sales.Domain

**Comando:**
```bash
dotnet new classlib -n GM.Sales.Domain -o src/Companies/GM/Sales/GM.Sales.Domain
```

**Estructura:**
```
GM.Sales.Domain/
├── Entities/
│   └── Order.cs
├── Interfaces/
│   └── IOrderRepository.cs
├── ValueObjects/
└── GM.Sales.Domain.csproj
```

**Dependencias:**
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\Shared\Shared.Contracts\Shared.Contracts.csproj" />
</ItemGroup>
```

#### 3.2. Crear GM.Sales.Infrastructure

**Comando:**
```bash
dotnet new classlib -n GM.Sales.Infrastructure -o src/Companies/GM/Sales/GM.Sales.Infrastructure
```

**Estructura:**
```
GM.Sales.Infrastructure/
├── Repositories/
│   └── OrderRepository.cs
└── GM.Sales.Infrastructure.csproj
```

**Dependencias:**
```xml
<ItemGroup>
  <ProjectReference Include="..\GM.Sales.Domain\GM.Sales.Domain.csproj" />
  <ProjectReference Include="..\..\..\..\Shared\Shared.Infrastructure\Shared.Infrastructure.csproj" />
</ItemGroup>
```

#### 3.3. Crear GM.Sales.Application

**Comando:**
```bash
dotnet new classlib -n GM.Sales.Application -o src/Companies/GM/Sales/GM.Sales.Application
```

**Estructura:**
```
GM.Sales.Application/
├── DTOs/
│   ├── OrderDto.cs
│   └── CreateOrderDto.cs
├── Services/
│   ├── IOrderService.cs
│   └── OrderService.cs
├── Exceptions/
│   └── OrderExceptions.cs
└── GM.Sales.Application.csproj
```

**Dependencias:**
```xml
<ItemGroup>
  <ProjectReference Include="..\GM.Sales.Domain\GM.Sales.Domain.csproj" />
  <ProjectReference Include="..\..\..\..\Shared\Shared.Contracts\Shared.Contracts.csproj" />
  <ProjectReference Include="..\..\..\..\Shared\Shared.Exceptions\Shared.Exceptions.csproj" />
</ItemGroup>
```

#### 3.4. Crear GM.Sales.API

**Comando:**
```bash
dotnet new webapi -n GM.Sales.API -o src/Companies/GM/Sales/GM.Sales.API
```

**Estructura:**
```
GM.Sales.API/
├── Controllers/
│   └── OrdersController.cs
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── appsettings.Production.json
└── GM.Sales.API.csproj
```

**Dependencias:**
```xml
<ItemGroup>
  <ProjectReference Include="..\GM.Sales.Application\GM.Sales.Application.csproj" />
  <ProjectReference Include="..\GM.Sales.Domain\GM.Sales.Domain.csproj" />
  <ProjectReference Include="..\..\..\..\Shared\Shared.Contracts\Shared.Contracts.csproj" />
  <ProjectReference Include="..\..\..\..\Shared\Shared.Exceptions\Shared.Exceptions.csproj" />
  <ProjectReference Include="..\..\..\..\Shared\Shared.Security\Shared.Security.csproj" />
  <ProjectReference Include="..\..\..\..\Shared\Shared.Infrastructure\Shared.Infrastructure.csproj" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.2" />
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.11" />
  <PackageReference Include="Serilog.AspNetCore" Version="8.0.4" />
  <PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="7.3.1" />
  <PackageReference Include="Scalar.AspNetCore" Version="2.11.6" />
</ItemGroup>
```

### Paso 4: Configurar Program.cs

**Plantilla base:**

```csharp
using GM.Sales.Application.Services;
using GM.Sales.Domain.Interfaces;
using GM.Sales.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;
using Shared.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ⚙️ Configurar URLs fijas para IIS
builder.WebHost.UseUrls("https://localhost:5001", "http://localhost:5000");

// ⚙️ Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
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
        Title = "GM Sales API",
        Version = "v1",
        Description = "API para gestión de ventas GM"
    });

    // Configurar JWT en Swagger
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
            ValidateIssuer = !string.IsNullOrWhiteSpace(jwtConfig.Issuer),
            ValidIssuer = string.IsNullOrWhiteSpace(jwtConfig.Issuer) ? null : jwtConfig.Issuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(jwtConfig.Audience),
            ValidAudience = string.IsNullOrWhiteSpace(jwtConfig.Audience) ? null : jwtConfig.Audience,
            ValidateLifetime = true
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
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

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

// ⚙️ Configurar pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    
    // Configurar Swagger UI
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GM Sales API v1");
        c.RoutePrefix = "swagger";
    });
    
    // Configurar Scalar
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("GM Sales API")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithTheme(ScalarTheme.BluePlanet);
    });
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

Log.Information("🚀 GM Sales API iniciada");

app.Run();

// ⚙️ Clase para configuración JWT
public class JwtConfig
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
}
```

### Paso 5: Crear appsettings.json

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "Oracle": "User Id=autos;Password=AUTOS9405CPS;Data Source=globaldmsdemo.dyndns.org/SISTEMAS;Min Pool Size=5;Max Pool Size=100;Connection Lifetime=300;Incr Pool Size=10;Connection Timeout=30;Pooling=True;"
  },
  "Jwt": {
    "Key": "GlobalDms@llavesupersercreta.noselacompartasanadie",
    "Issuer": "",
    "Audience": "",
    "Subject": "GlobalDMS"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  },
  "AllowedHosts": "*"
}
```

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "Oracle": "User Id=autos;Password=AUTOS9405CPS;Data Source=globaldmsdemo.dyndns.org/SISTEMAS;Min Pool Size=5;Max Pool Size=100;Connection Lifetime=300;Incr Pool Size=10;Connection Timeout=30;Pooling=True;"
  },
  "Jwt": {
    "Key": "GlobalDms@llavesupersercreta.noselacompartasanadie",
    "Issuer": "",
    "Audience": "",
    "Subject": "GlobalDMS"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  },
  "AllowedHosts": "*"
}
```

**appsettings.Production.json:**
```json
{
  "ConnectionStrings": {
    "Oracle": "User Id=autos;Password=AUTOS9405CPS;Data Source=globaldmsdemo.dyndns.org/SISTEMAS;Min Pool Size=5;Max Pool Size=100;Connection Lifetime=300;Incr Pool Size=10;Connection Timeout=30;Pooling=True;"
  },
  "Jwt": {
    "Key": "GlobalDms@llavesupersercreta.noselacompartasanadie",
    "Issuer": "",
    "Audience": "",
    "Subject": "GlobalDMS"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "Logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://localhost:5001"
      },
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  }
}
```

### Paso 6: Crear Controller de Ejemplo

**OrdersController.cs:**
```csharp
using GM.Sales.Application.Services;
using GM.Sales.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;

namespace GM.Sales.API.Controllers;

[ApiController]
[Route("api/v1/gm/sales/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetOrders()
    {
        try
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(ApiResponse<List<OrderDto>>.Success(orders));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener órdenes");
            return StatusCode(500, ApiResponse<List<OrderDto>>.Error("Error al obtener órdenes"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrder(int id)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound(ApiResponse<OrderDto>.Error("Orden no encontrada"));
            }
            return Ok(ApiResponse<OrderDto>.Success(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener orden {OrderId}", id);
            return StatusCode(500, ApiResponse<OrderDto>.Error("Error al obtener orden"));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            var order = await _orderService.CreateOrderAsync(dto);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, 
                ApiResponse<OrderDto>.Success(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear orden");
            return StatusCode(500, ApiResponse<OrderDto>.Error("Error al crear orden"));
        }
    }
}
```

### Paso 7: Agregar al Solution

**Comando:**
```bash
dotnet sln add src/Companies/GM/Sales/GM.Sales.API/GM.Sales.API.csproj
dotnet sln add src/Companies/GM/Sales/GM.Sales.Application/GM.Sales.Application.csproj
dotnet sln add src/Companies/GM/Sales/GM.Sales.Domain/GM.Sales.Domain.csproj
dotnet sln add src/Companies/GM/Sales/GM.Sales.Infrastructure/GM.Sales.Infrastructure.csproj
```

---

## ✅ Checklist de Creación

- [ ] Estructura de carpetas creada
- [ ] 4 proyectos creados (API, Application, Domain, Infrastructure)
- [ ] Dependencias configuradas correctamente
- [ ] Program.cs configurado con Swagger y Scalar
- [ ] appsettings.json creados (base, Development, Production)
- [ ] Controller de ejemplo creado
- [ ] Proyectos agregados al solution
- [ ] Compila sin errores
- [ ] Swagger y Scalar funcionan correctamente

---

## 🎯 Ejemplo Completo: Jetour.Sales

### Estructura Final

```
src/Companies/Jetour/Sales/
├── Jetour.Sales.API/
│   ├── Controllers/
│   │   └── OrdersController.cs
│   ├── Program.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── appsettings.Production.json
│   └── Jetour.Sales.API.csproj
│
├── Jetour.Sales.Application/
│   ├── DTOs/
│   │   ├── OrderDto.cs
│   │   └── CreateOrderDto.cs
│   ├── Services/
│   │   ├── IOrderService.cs
│   │   └── OrderService.cs
│   └── Jetour.Sales.Application.csproj
│
├── Jetour.Sales.Domain/
│   ├── Entities/
│   │   └── Order.cs
│   ├── Interfaces/
│   │   └── IOrderRepository.cs
│   └── Jetour.Sales.Domain.csproj
│
└── Jetour.Sales.Infrastructure/
    ├── Repositories/
    │   └── OrderRepository.cs
    └── Jetour.Sales.Infrastructure.csproj
```

### Endpoints Resultantes

```
GET    /api/v1/jetour/sales/orders
GET    /api/v1/jetour/sales/orders/{id}
POST   /api/v1/jetour/sales/orders
PUT    /api/v1/jetour/sales/orders/{id}
DELETE /api/v1/jetour/sales/orders/{id}
```

---

**Última actualización:** 2025-01-16
**Versión:** 1.0

