# 📋 Guía de Implementación: Controladores de Monitoreo y Mantenimiento

## 🎯 Objetivo

Este documento explica cómo implementar en **GlobalOracleAPI** los controladores de monitoreo y mantenimiento que mantienen las conexiones Oracle activas y monitorean el rendimiento de la aplicación.

---

## 📦 Controladores a Implementar

1. **PerformanceController** - Monitoreo de rendimiento de la aplicación
2. **ConnectionPoolController** - Monitoreo y mantenimiento del pool de conexiones Oracle
3. **DiagnosticoController** - Diagnóstico y validación de conexiones

---

## 🏗️ Estructura Propuesta

```
GlobalOracleAPI/
├── src/
│   ├── Shared/
│   │   └── Shared.Infrastructure/
│   │       ├── Services/
│   │       │   ├── PerformanceMonitor.cs          # Servicio en segundo plano
│   │       │   └── ConnectionPoolMaintenance.cs   # Servicio en segundo plano
│   │       └── Helpers/
│   │           └── PerformanceStats.cs            # DTOs para estadísticas
│   │
│   └── Companies/
│       └── GM/
│           └── CatalogSync/
│               └── GM.CatalogSync.API/
│                   └── Controllers/
│                       ├── Performance/
│                       │   └── PerformanceController.cs
│                       ├── ConnectionPool/
│                       │   └── ConnectionPoolController.cs
│                       └── Diagnostico/
│                           └── DiagnosticoController.cs
```

**Nota:** Los servicios en segundo plano van en `Shared.Infrastructure` porque son comunes a todos los módulos.

---

## 1️⃣ PerformanceController

### 📍 Ubicación
`GM.CatalogSync.API/Controllers/Performance/PerformanceController.cs`

### 🎯 Funcionalidad
- Monitoreo de rendimiento de la aplicación
- Estadísticas de memoria, CPU, threads
- Health checks
- Optimización de memoria

### 📝 Endpoints

```
GET  /api/v1/gm/catalog-sync/performance/stats
GET  /api/v1/gm/catalog-sync/performance/health
POST /api/v1/gm/catalog-sync/performance/optimize
```

### 🔧 Dependencias
- `PerformanceMonitor` (servicio en segundo plano)
- `ILogger<PerformanceController>`

---

## 2️⃣ ConnectionPoolController

### 📍 Ubicación
`GM.CatalogSync.API/Controllers/ConnectionPool/ConnectionPoolController.cs`

### 🎯 Funcionalidad
- Monitoreo del pool de conexiones Oracle
- Warm-up manual de conexiones
- Health checks del pool
- Optimización del pool
- Estadísticas de mantenimiento

### 📝 Endpoints

```
GET  /api/v1/gm/catalog-sync/connection-pool/stats
POST /api/v1/gm/catalog-sync/connection-pool/warmup
POST /api/v1/gm/catalog-sync/connection-pool/health-check
POST /api/v1/gm/catalog-sync/connection-pool/optimize
GET  /api/v1/gm/catalog-sync/connection-pool/pool-info
```

### 🔧 Dependencias
- `ConnectionPoolMaintenance` (servicio en segundo plano)
- `IOracleConnectionFactory` (para ejecutar queries de warm-up)
- `ILogger<ConnectionPoolController>`

---

## 3️⃣ DiagnosticoController

### 📍 Ubicación
`GM.CatalogSync.API/Controllers/Diagnostico/DiagnosticoController.cs`

### 🎯 Funcionalidad
- Validación de conexiones específicas
- Validación de todas las conexiones
- Estadísticas del pool de conexiones
- Diagnóstico de salud de la base de datos

### 📝 Endpoints

```
GET  /api/v1/gm/catalog-sync/diagnostico/validar-conexion/{connectionId}
GET  /api/v1/gm/catalog-sync/diagnostico/estadisticas-pool/{connectionId}
GET  /api/v1/gm/catalog-sync/diagnostico/validar-todas-conexiones
```

### 🔧 Dependencias
- `IOracleConnectionFactory`
- `ILogger<DiagnosticoController>`

---

## ⚙️ Servicios en Segundo Plano (Background Services)

### 🔥 PerformanceMonitor

**Ubicación:** `Shared.Infrastructure/Services/PerformanceMonitor.cs`

**Funcionalidad:**
- Mantiene la aplicación activa (evita cold starts)
- Monitoreo continuo de rendimiento
- Health checks periódicos

**Timers:**
- **Keep-Alive**: Cada 5 minutos
- **Health Check**: Cada 2 minutos

**Registro en Program.cs:**
```csharp
builder.Services.AddSingleton<PerformanceMonitor>();
builder.Services.AddHostedService<PerformanceMonitor>(provider => 
    provider.GetRequiredService<PerformanceMonitor>());
```

---

### 🔥 ConnectionPoolMaintenance

**Ubicación:** `Shared.Infrastructure/Services/ConnectionPoolMaintenance.cs`

**Funcionalidad:**
- Mantiene las conexiones Oracle calientes (warm-up)
- Health checks continuos del pool
- Optimización periódica del pool

**Timers:**
- **Warm-up**: Cada 2 minutos (inicia después de 30 segundos)
- **Health Check**: Cada 1 minuto (inicia después de 15 segundos)
- **Optimización**: Cada 3 minutos (inicia después de 45 segundos)

**Registro en Program.cs:**
```csharp
builder.Services.AddSingleton<ConnectionPoolMaintenance>();
builder.Services.AddHostedService<ConnectionPoolMaintenance>(provider => 
    provider.GetRequiredService<ConnectionPoolMaintenance>());
```

**⚠️ IMPORTANTE:** Este servicio ejecuta queries periódicas (`SELECT 1 FROM DUAL` y queries de login) para mantener las conexiones activas y evitar timeouts.

---

## 📝 Implementación Paso a Paso

### Paso 1: Crear Servicios en Shared.Infrastructure

#### 1.1 PerformanceMonitor.cs

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Shared.Infrastructure.Services
{
    /// <summary>
    /// Servicio para monitorear el rendimiento y evitar cold starts
    /// </summary>
    public class PerformanceMonitor : IHostedService
    {
        private readonly ILogger<PerformanceMonitor> _logger;
        private readonly Timer _keepAliveTimer;
        private readonly Timer _healthCheckTimer;
        private readonly Stopwatch _uptimeStopwatch;

        public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
        {
            _logger = logger;
            _uptimeStopwatch = Stopwatch.StartNew();
            
            // Timer para mantener la aplicación activa (cada 5 minutos)
            _keepAliveTimer = new Timer(KeepAliveCallback, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            
            // Timer para verificar el estado de la aplicación (cada 2 minutos)
            _healthCheckTimer = new Timer(HealthCheckCallback, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
            
            _logger.LogInformation("🚀 PerformanceMonitor iniciado - Tiempo de inicio: {TiempoInicio}", DateTime.Now);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("✅ PerformanceMonitor servicio iniciado exitosamente");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _uptimeStopwatch.Stop();
            _keepAliveTimer?.Dispose();
            _healthCheckTimer?.Dispose();
            
            _logger.LogInformation("🛑 PerformanceMonitor servicio detenido - Tiempo total activo: {TiempoTotal}ms", 
                _uptimeStopwatch.ElapsedMilliseconds);
            
            return Task.CompletedTask;
        }

        private void KeepAliveCallback(object? state)
        {
            try
            {
                var uptime = _uptimeStopwatch.Elapsed;
                _logger.LogDebug("💓 Keep-Alive: Aplicación activa desde hace {TiempoUptime} (Tiempo total: {TiempoTotal}ms)", 
                    uptime.ToString(@"dd\.hh\:mm\:ss"), _uptimeStopwatch.ElapsedMilliseconds);
                
                var process = Process.GetCurrentProcess();
                var memoryMB = process.WorkingSet64 / 1024 / 1024;
                _logger.LogDebug("📊 Memoria utilizada: {MemoriaMB} MB", memoryMB);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en KeepAliveCallback: {Error}", ex.Message);
            }
        }

        private void HealthCheckCallback(object? state)
        {
            try
            {
                var uptime = _uptimeStopwatch.Elapsed;
                var process = Process.GetCurrentProcess();
                var memoryMB = process.WorkingSet64 / 1024 / 1024;
                var cpuTime = process.TotalProcessorTime;
                
                _logger.LogInformation("🏥 Health Check: Uptime: {TiempoUptime}, Memoria: {MemoriaMB} MB, CPU: {CpuTime}", 
                    uptime.ToString(@"dd\.hh\:mm\:ss"), memoryMB, cpuTime);
                
                if (memoryMB > 500)
                {
                    _logger.LogWarning("⚠️ Alto uso de memoria detectado: {MemoriaMB} MB", memoryMB);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en HealthCheckCallback: {Error}", ex.Message);
            }
        }

        public PerformanceStats GetPerformanceStats()
        {
            var process = Process.GetCurrentProcess();
            
            return new PerformanceStats
            {
                Uptime = _uptimeStopwatch.Elapsed,
                UptimeMilliseconds = _uptimeStopwatch.ElapsedMilliseconds,
                MemoryUsageMB = process.WorkingSet64 / 1024 / 1024,
                CpuTime = process.TotalProcessorTime,
                ThreadCount = process.Threads.Count,
                ProcessId = process.Id,
                StartTime = process.StartTime
            };
        }
    }

    public class PerformanceStats
    {
        public TimeSpan Uptime { get; set; }
        public long UptimeMilliseconds { get; set; }
        public long MemoryUsageMB { get; set; }
        public TimeSpan CpuTime { get; set; }
        public int ThreadCount { get; set; }
        public int ProcessId { get; set; }
        public DateTime StartTime { get; set; }
    }
}
```

#### 1.2 ConnectionPoolMaintenance.cs

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure;
using Shared.Security;
using System.Diagnostics;
using Oracle.ManagedDataAccess.Client;
using Dapper;

namespace Shared.Infrastructure.Services
{
    /// <summary>
    /// Servicio para mantener el pool de conexiones Oracle caliente y eficiente
    /// </summary>
    public class ConnectionPoolMaintenance : IHostedService, IDisposable
    {
        private readonly ILogger<ConnectionPoolMaintenance> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Timer _warmupTimer;
        private readonly Timer _healthCheckTimer;
        private readonly Timer _poolOptimizationTimer;
        private readonly Stopwatch _uptimeStopwatch;
        
        // Configuración de intervalos
        private readonly int _warmupIntervalMinutes = 2; // Cada 2 minutos
        private readonly int _healthCheckIntervalMinutes = 1; // Cada 1 minuto
        private readonly int _poolOptimizationIntervalMinutes = 3; // Cada 3 minutos
        
        // Estadísticas
        private int _totalWarmupCycles = 0;
        private int _totalHealthChecks = 0;
        private int _totalOptimizations = 0;
        private DateTime _lastWarmupTime = DateTime.MinValue;
        private DateTime _lastHealthCheckTime = DateTime.MinValue;
        private DateTime _lastOptimizationTime = DateTime.MinValue;

        public ConnectionPoolMaintenance(
            ILogger<ConnectionPoolMaintenance> logger, 
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _uptimeStopwatch = Stopwatch.StartNew();
            
            // Timer para warm-up de conexiones (inicia después de 30 segundos, luego cada 2 minutos)
            _warmupTimer = new Timer(WarmupConnections, null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(_warmupIntervalMinutes));
            
            // Timer para health checks continuos (inicia después de 15 segundos, luego cada 1 minuto)
            _healthCheckTimer = new Timer(PerformHealthChecks, null, TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(_healthCheckIntervalMinutes));
            
            // Timer para optimización del pool (inicia después de 45 segundos, luego cada 3 minutos)
            _poolOptimizationTimer = new Timer(OptimizeConnectionPool, null, TimeSpan.FromSeconds(45), TimeSpan.FromMinutes(_poolOptimizationIntervalMinutes));
            
            _logger.LogInformation("🔥 ConnectionPoolMaintenance iniciado - Warm-up cada {WarmupInterval}min, Health check cada {HealthInterval}min, Optimización cada {OptimizationInterval}min", 
                _warmupIntervalMinutes, _healthCheckIntervalMinutes, _poolOptimizationIntervalMinutes);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("✅ ConnectionPoolMaintenance servicio iniciado exitosamente");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _uptimeStopwatch.Stop();
            _warmupTimer?.Dispose();
            _healthCheckTimer?.Dispose();
            _poolOptimizationTimer?.Dispose();
            
            _logger.LogInformation("🛑 ConnectionPoolMaintenance servicio detenido - Tiempo total activo: {TiempoTotal}ms", 
                _uptimeStopwatch.ElapsedMilliseconds);
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Mantiene las conexiones calientes ejecutando queries simples
        /// </summary>
        private async void WarmupConnections(object? state)
        {
            var warmupStopwatch = Stopwatch.StartNew();
            try
            {
                _totalWarmupCycles++;
                _lastWarmupTime = DateTime.Now;
                
                var warmupCode = CorrelationHelper.GenerateEndpointId($"WARMUP_CYCLE_{_totalWarmupCycles}");
                
                _logger.LogInformation("🔥 [{WarmupCode}] ===== INICIANDO WARM-UP DE CONEXIONES ORACLE - CICLO #{Ciclo} =====", 
                    warmupCode, _totalWarmupCycles);
                
                using (var scope = _serviceProvider.CreateScope())
                {
                    var connectionFactory = scope.ServiceProvider.GetRequiredService<IOracleConnectionFactory>();
                    
                    // Ejecutar warm-up en la conexión principal
                    await WarmupConnection(connectionFactory, warmupCode);
                }
                
                warmupStopwatch.Stop();
                _logger.LogInformation("🔥 [{WarmupCode}] ===== WARM-UP COMPLETADO EXITOSAMENTE =====", warmupCode);
                _logger.LogInformation("🔥 [{WarmupCode}] Tiempo total: {TiempoWarmup}ms - Ciclo #{Ciclo}", 
                    warmupCode, warmupStopwatch.ElapsedMilliseconds, _totalWarmupCycles);
            }
            catch (Exception ex)
            {
                warmupStopwatch.Stop();
                var errorCode = CorrelationHelper.GenerateEndpointId($"WARMUP_ERROR_{_totalWarmupCycles}");
                _logger.LogError(ex, "❌ [{ErrorCode}] Error en warm-up después de {TiempoWarmup}ms - Ciclo #{Ciclo}: {Error}", 
                    errorCode, warmupStopwatch.ElapsedMilliseconds, _totalWarmupCycles, ex.Message);
            }
        }

        /// <summary>
        /// Mantiene una conexión específica caliente ejecutando queries reales
        /// </summary>
        private async Task WarmupConnection(IOracleConnectionFactory connectionFactory, string correlationId)
        {
            var connectionStopwatch = Stopwatch.StartNew();
            try
            {
                _logger.LogInformation("🔥 [{CorrelationId}] ===== INICIANDO WARM-UP CONEXIÓN =====", correlationId);
                
                // 1. Query básica de validación
                var validationStopwatch = Stopwatch.StartNew();
                using var connection = await connectionFactory.CreateConnectionAsync();
                
                var validationQuery = "SELECT 1 FROM DUAL";
                var result = await connection.QueryFirstOrDefaultAsync<int>(validationQuery);
                
                validationStopwatch.Stop();
                
                if (result == 1)
                {
                    _logger.LogInformation("🔥 [{CorrelationId}] ✅ [WARM-UP] Validación básica exitosa en {TiempoValidacion}ms", 
                        correlationId, validationStopwatch.ElapsedMilliseconds);
                    
                    connectionStopwatch.Stop();
                    _logger.LogInformation("🔥 [{CorrelationId}] 🎉 ===== WARM-UP COMPLETADO EXITOSAMENTE =====", correlationId);
                    _logger.LogInformation("🔥 [{CorrelationId}] 📊 Tiempo total: {TiempoTotal}ms", 
                        correlationId, connectionStopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogWarning("🔥 [{CorrelationId}] ❌ Error en validación básica", correlationId);
                }
            }
            catch (Exception ex)
            {
                connectionStopwatch.Stop();
                _logger.LogError(ex, "❌ [{CorrelationId}] Error crítico en warm-up después de {TiempoConexion}ms: {Error}", 
                    correlationId, connectionStopwatch.ElapsedMilliseconds, ex.Message);
            }
        }

        /// <summary>
        /// Realiza health checks continuos del pool de conexiones
        /// </summary>
        private async void PerformHealthChecks(object? state)
        {
            var healthCheckStopwatch = Stopwatch.StartNew();
            try
            {
                _totalHealthChecks++;
                _lastHealthCheckTime = DateTime.Now;
                
                _logger.LogDebug("🏥 Iniciando health check del pool de conexiones - Check #{Check}", _totalHealthChecks);
                
                using (var scope = _serviceProvider.CreateScope())
                {
                    var connectionFactory = scope.ServiceProvider.GetRequiredService<IOracleConnectionFactory>();
                    
                    var isHealthy = await CheckConnectionHealth(connectionFactory);
                    
                    healthCheckStopwatch.Stop();
                    
                    if (isHealthy)
                    {
                        _logger.LogInformation("🏥 Health check completado - Conexión saludable en {TiempoHealthCheck}ms", 
                            healthCheckStopwatch.ElapsedMilliseconds);
                    }
                    else
                    {
                        _logger.LogWarning("🏥 Health check - Conexión no saludable en {TiempoHealthCheck}ms", 
                            healthCheckStopwatch.ElapsedMilliseconds);
                    }
                }
            }
            catch (Exception ex)
            {
                healthCheckStopwatch.Stop();
                _logger.LogError(ex, "❌ Error en health check después de {TiempoHealthCheck}ms - Check #{Check}: {Error}", 
                    healthCheckStopwatch.ElapsedMilliseconds, _totalHealthChecks, ex.Message);
            }
        }

        /// <summary>
        /// Verifica la salud de una conexión específica
        /// </summary>
        private async Task<bool> CheckConnectionHealth(IOracleConnectionFactory connectionFactory)
        {
            try
            {
                using var connection = await connectionFactory.CreateConnectionAsync();
                var result = await connection.QueryFirstOrDefaultAsync<int>("SELECT 1 FROM DUAL");
                return result == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al verificar salud de conexión: {Error}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Optimiza el pool de conexiones periódicamente
        /// </summary>
        private void OptimizeConnectionPool(object? state)
        {
            var optimizationStopwatch = Stopwatch.StartNew();
            try
            {
                _totalOptimizations++;
                _lastOptimizationTime = DateTime.Now;
                
                _logger.LogInformation("⚡ Iniciando optimización del pool de conexiones - Optimización #{Optimization}", _totalOptimizations);
                
                // Forzar garbage collection para liberar memoria
                var beforeGC = GC.GetTotalMemory(false);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                var afterGC = GC.GetTotalMemory(false);
                
                var memoryFreed = beforeGC - afterGC;
                var memoryFreedMB = memoryFreed / 1024 / 1024;
                
                optimizationStopwatch.Stop();
                
                _logger.LogInformation("⚡ Optimización del pool completada en {TiempoOptimizacion}ms - Memoria liberada: {MemoriaLiberada} MB - Optimización #{Optimization}", 
                    optimizationStopwatch.ElapsedMilliseconds, memoryFreedMB, _totalOptimizations);
            }
            catch (Exception ex)
            {
                optimizationStopwatch.Stop();
                _logger.LogError(ex, "❌ Error en optimización del pool después de {TiempoOptimizacion}ms - Optimización #{Optimization}: {Error}", 
                    optimizationStopwatch.ElapsedMilliseconds, _totalOptimizations, ex.Message);
            }
        }

        /// <summary>
        /// Obtiene estadísticas del servicio de mantenimiento
        /// </summary>
        public ConnectionMaintenanceStats GetMaintenanceStats()
        {
            var process = Process.GetCurrentProcess();
            
            return new ConnectionMaintenanceStats
            {
                Uptime = _uptimeStopwatch.Elapsed,
                UptimeMilliseconds = _uptimeStopwatch.ElapsedMilliseconds,
                MemoryUsageMB = process.WorkingSet64 / 1024 / 1024,
                TotalWarmupCycles = _totalWarmupCycles,
                TotalHealthChecks = _totalHealthChecks,
                TotalOptimizations = _totalOptimizations,
                LastWarmupTime = _lastWarmupTime,
                LastHealthCheckTime = _lastHealthCheckTime,
                LastOptimizationTime = _lastOptimizationTime,
                WarmupIntervalMinutes = _warmupIntervalMinutes,
                HealthCheckIntervalMinutes = _healthCheckIntervalMinutes,
                PoolOptimizationIntervalMinutes = _poolOptimizationIntervalMinutes
            };
        }

        public void Dispose()
        {
            _warmupTimer?.Dispose();
            _healthCheckTimer?.Dispose();
            _poolOptimizationTimer?.Dispose();
        }
    }

    public class ConnectionMaintenanceStats
    {
        public TimeSpan Uptime { get; set; }
        public long UptimeMilliseconds { get; set; }
        public long MemoryUsageMB { get; set; }
        public int TotalWarmupCycles { get; set; }
        public int TotalHealthChecks { get; set; }
        public int TotalOptimizations { get; set; }
        public DateTime LastWarmupTime { get; set; }
        public DateTime LastHealthCheckTime { get; set; }
        public DateTime LastOptimizationTime { get; set; }
        public int WarmupIntervalMinutes { get; set; }
        public int HealthCheckIntervalMinutes { get; set; }
        public int PoolOptimizationIntervalMinutes { get; set; }
    }
}
```

---

### Paso 2: Crear Controllers

#### 2.1 PerformanceController.cs

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Infrastructure.Services;
using Shared.Security;
using System.Diagnostics;

namespace GM.CatalogSync.API.Controllers.Performance
{
    [ApiController]
    [Route("api/v1/gm/catalog-sync/performance")]
    [Produces("application/json")]
    [Authorize]
    public class PerformanceController : ControllerBase
    {
        private readonly ILogger<PerformanceController> _logger;
        private readonly PerformanceMonitor? _performanceMonitor;
        private readonly Stopwatch _requestStopwatch;

        public PerformanceController(
            ILogger<PerformanceController> logger, 
            PerformanceMonitor? performanceMonitor = null)
        {
            _logger = logger;
            _performanceMonitor = performanceMonitor;
            _requestStopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Obtiene estadísticas de rendimiento de la aplicación
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public IActionResult GetPerformanceStats()
        {
            _requestStopwatch.Restart();
            _logger.LogInformation("📊 Solicitando estadísticas de rendimiento");

            try
            {
                var process = Process.GetCurrentProcess();
                var performanceStats = _performanceMonitor?.GetPerformanceStats();

                var stats = new
                {
                    ProcessId = process.Id,
                    ProcessName = process.ProcessName,
                    StartTime = process.StartTime,
                    Uptime = performanceStats?.Uptime ?? TimeSpan.Zero,
                    UptimeFormatted = performanceStats?.Uptime.ToString(@"dd\.hh\:mm\:ss") ?? "N/A",
                    MemoryUsageMB = performanceStats?.MemoryUsageMB ?? (process.WorkingSet64 / 1024 / 1024),
                    CpuTime = performanceStats?.CpuTime ?? process.TotalProcessorTime,
                    ThreadCount = performanceStats?.ThreadCount ?? process.Threads.Count,
                    MachineName = Environment.MachineName,
                    ProcessorCount = Environment.ProcessorCount,
                    OSVersion = Environment.OSVersion.ToString(),
                    FrameworkVersion = Environment.Version.ToString(),
                    RequestResponseTimeMs = _requestStopwatch.ElapsedMilliseconds,
                    Timestamp = DateTime.UtcNow
                };

                _requestStopwatch.Stop();
                _logger.LogInformation("📊 Estadísticas de rendimiento generadas en {TiempoRespuesta}ms", 
                    _requestStopwatch.ElapsedMilliseconds);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = stats,
                    Message = "Estadísticas de rendimiento obtenidas exitosamente",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
            catch (Exception ex)
            {
                _requestStopwatch.Stop();
                _logger.LogError(ex, "❌ Error al obtener estadísticas de rendimiento: {Error}", ex.Message);
                
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error al obtener estadísticas de rendimiento",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }

        /// <summary>
        /// Endpoint de health check simple
        /// </summary>
        [HttpGet("health")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public IActionResult HealthCheck()
        {
            _requestStopwatch.Restart();
            
            try
            {
                var process = Process.GetCurrentProcess();
                var memoryMB = process.WorkingSet64 / 1024 / 1024;
                var uptime = DateTime.Now - process.StartTime;
                
                _requestStopwatch.Stop();
                
                var health = new
                {
                    Status = "Healthy",
                    Uptime = uptime.ToString(@"dd\.hh\:mm\:ss"),
                    MemoryUsageMB = memoryMB,
                    ResponseTimeMs = _requestStopwatch.ElapsedMilliseconds,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("🏥 Health check completado en {TiempoRespuesta}ms - Status: {Status}", 
                    _requestStopwatch.ElapsedMilliseconds, health.Status);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = health,
                    Message = "Health check completado",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
            catch (Exception ex)
            {
                _requestStopwatch.Stop();
                _logger.LogError(ex, "❌ Error en health check: {Error}", ex.Message);
                
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error en health check",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }

        /// <summary>
        /// Endpoint para forzar garbage collection y optimizar memoria
        /// </summary>
        [HttpPost("optimize")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public IActionResult OptimizeMemory()
        {
            _requestStopwatch.Restart();
            _logger.LogInformation("🔧 Iniciando optimización de memoria");

            try
            {
                var beforeGC = GC.GetTotalMemory(false);
                var beforeMemoryMB = beforeGC / 1024 / 1024;

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var afterGC = GC.GetTotalMemory(false);
                var afterMemoryMB = afterGC / 1024 / 1024;
                var memoryFreedMB = beforeMemoryMB - afterMemoryMB;

                _requestStopwatch.Stop();

                var result = new
                {
                    MemoryBeforeMB = beforeMemoryMB,
                    MemoryAfterMB = afterMemoryMB,
                    MemoryFreedMB = memoryFreedMB,
                    OptimizationTimeMs = _requestStopwatch.ElapsedMilliseconds,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("🔧 Optimización de memoria completada en {TiempoOptimizacion}ms - Memoria liberada: {MemoriaLiberada} MB", 
                    _requestStopwatch.ElapsedMilliseconds, memoryFreedMB);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = result,
                    Message = $"Optimización completada - Memoria liberada: {memoryFreedMB} MB",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
            catch (Exception ex)
            {
                _requestStopwatch.Stop();
                _logger.LogError(ex, "❌ Error en optimización de memoria: {Error}", ex.Message);
                
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error en optimización de memoria",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }
    }
}
```

#### 2.2 ConnectionPoolController.cs

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Infrastructure;
using Shared.Infrastructure.Services;
using Shared.Security;
using System.Diagnostics;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace GM.CatalogSync.API.Controllers.ConnectionPool
{
    [ApiController]
    [Route("api/v1/gm/catalog-sync/connection-pool")]
    [Produces("application/json")]
    [Authorize]
    public class ConnectionPoolController : ControllerBase
    {
        private readonly ILogger<ConnectionPoolController> _logger;
        private readonly ConnectionPoolMaintenance? _connectionMaintenance;
        private readonly IServiceProvider _serviceProvider;
        private readonly Stopwatch _requestStopwatch;

        public ConnectionPoolController(
            ILogger<ConnectionPoolController> logger,
            IServiceProvider serviceProvider,
            ConnectionPoolMaintenance? connectionMaintenance = null)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _connectionMaintenance = connectionMaintenance;
            _requestStopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Obtiene estadísticas del pool de conexiones y mantenimiento
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public IActionResult GetConnectionPoolStats()
        {
            _requestStopwatch.Restart();
            _logger.LogInformation("📊 Solicitando estadísticas del pool de conexiones");

            try
            {
                var process = Process.GetCurrentProcess();
                var maintenanceStats = _connectionMaintenance?.GetMaintenanceStats();

                var stats = new
                {
                    ProcessId = process.Id,
                    ProcessName = process.ProcessName,
                    StartTime = process.StartTime,
                    Uptime = maintenanceStats?.Uptime ?? TimeSpan.Zero,
                    UptimeFormatted = maintenanceStats?.Uptime.ToString(@"dd\.hh\:mm\:ss") ?? "N/A",
                    MemoryUsageMB = maintenanceStats?.MemoryUsageMB ?? (process.WorkingSet64 / 1024 / 1024),
                    MaintenanceStats = maintenanceStats != null ? new
                    {
                        TotalWarmupCycles = maintenanceStats.TotalWarmupCycles,
                        TotalHealthChecks = maintenanceStats.TotalHealthChecks,
                        TotalOptimizations = maintenanceStats.TotalOptimizations,
                        LastWarmupTime = maintenanceStats.LastWarmupTime,
                        LastHealthCheckTime = maintenanceStats.LastHealthCheckTime,
                        LastOptimizationTime = maintenanceStats.LastOptimizationTime,
                        WarmupIntervalMinutes = maintenanceStats.WarmupIntervalMinutes,
                        HealthCheckIntervalMinutes = maintenanceStats.HealthCheckIntervalMinutes,
                        PoolOptimizationIntervalMinutes = maintenanceStats.PoolOptimizationIntervalMinutes
                    } : null,
                    RequestResponseTimeMs = _requestStopwatch.ElapsedMilliseconds,
                    Timestamp = DateTime.UtcNow
                };

                _requestStopwatch.Stop();
                _logger.LogInformation("📊 Estadísticas del pool de conexiones generadas en {TiempoRespuesta}ms", 
                    _requestStopwatch.ElapsedMilliseconds);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = stats,
                    Message = "Estadísticas del pool de conexiones obtenidas exitosamente",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
            catch (Exception ex)
            {
                _requestStopwatch.Stop();
                _logger.LogError(ex, "❌ Error al obtener estadísticas del pool de conexiones: {Error}", ex.Message);
                
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error al obtener estadísticas del pool de conexiones",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }

        /// <summary>
        /// Realiza un warm-up manual del pool de conexiones
        /// </summary>
        [HttpPost("warmup")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> PerformManualWarmup()
        {
            _requestStopwatch.Restart();
            _logger.LogInformation("🔥 Iniciando warm-up manual del pool de conexiones");

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var connectionFactory = scope.ServiceProvider.GetRequiredService<IOracleConnectionFactory>();
                    
                    var warmupStopwatch = Stopwatch.StartNew();
                    using var connection = await connectionFactory.CreateConnectionAsync();
                    
                    var result = await connection.QueryFirstOrDefaultAsync<int>("SELECT 1 FROM DUAL");
                    warmupStopwatch.Stop();
                    
                    _requestStopwatch.Stop();
                    
                    _logger.LogInformation("🔥 Warm-up manual completado en {TiempoWarmup}ms", 
                        _requestStopwatch.ElapsedMilliseconds);

                    return Ok(new ApiResponse<object>
                    {
                        Success = result == 1,
                        Data = new
                        {
                            Success = result == 1,
                            ResponseTimeMs = warmupStopwatch.ElapsedMilliseconds,
                            Timestamp = DateTime.UtcNow
                        },
                        Message = result == 1 ? "Warm-up manual completado exitosamente" : "Warm-up falló",
                        Timestamp = DateTimeHelper.GetMexicoTimeString()
                    });
                }
            }
            catch (Exception ex)
            {
                _requestStopwatch.Stop();
                _logger.LogError(ex, "❌ Error en warm-up manual: {Error}", ex.Message);
                
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error al realizar warm-up manual del pool de conexiones",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }

        /// <summary>
        /// Realiza un health check manual del pool de conexiones
        /// </summary>
        [HttpPost("health-check")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> PerformManualHealthCheck()
        {
            _requestStopwatch.Restart();
            _logger.LogInformation("🏥 Iniciando health check manual del pool de conexiones");

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var connectionFactory = scope.ServiceProvider.GetRequiredService<IOracleConnectionFactory>();
                    
                    var healthStopwatch = Stopwatch.StartNew();
                    using var connection = await connectionFactory.CreateConnectionAsync();
                    var result = await connection.QueryFirstOrDefaultAsync<int>("SELECT 1 FROM DUAL");
                    healthStopwatch.Stop();
                    
                    var isHealthy = result == 1;
                    
                    _requestStopwatch.Stop();
                    
                    _logger.LogInformation("🏥 Health check manual completado en {TiempoHealthCheck}ms - Status: {Status}", 
                        _requestStopwatch.ElapsedMilliseconds, isHealthy ? "Healthy" : "Unhealthy");

                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Data = new
                        {
                            IsHealthy = isHealthy,
                            Status = isHealthy ? "Healthy" : "Unhealthy",
                            ResponseTimeMs = healthStopwatch.ElapsedMilliseconds,
                            Timestamp = DateTime.UtcNow
                        },
                        Message = isHealthy ? "Health check completado - Conexión saludable" : "Health check completado - Conexión no saludable",
                        Timestamp = DateTimeHelper.GetMexicoTimeString()
                    });
                }
            }
            catch (Exception ex)
            {
                _requestStopwatch.Stop();
                _logger.LogError(ex, "❌ Error en health check manual: {Error}", ex.Message);
                
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error al realizar health check manual del pool de conexiones",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }

        /// <summary>
        /// Optimiza manualmente el pool de conexiones
        /// </summary>
        [HttpPost("optimize")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public IActionResult PerformManualOptimization()
        {
            _requestStopwatch.Restart();
            _logger.LogInformation("⚡ Iniciando optimización manual del pool de conexiones");

            try
            {
                var beforeGC = GC.GetTotalMemory(false);
                var beforeMemoryMB = beforeGC / 1024 / 1024;

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                var afterGC = GC.GetTotalMemory(false);
                var afterMemoryMB = afterGC / 1024 / 1024;
                var memoryFreedMB = beforeMemoryMB - afterMemoryMB;

                _requestStopwatch.Stop();

                _logger.LogInformation("⚡ Optimización manual del pool completada en {TiempoOptimizacion}ms - Memoria liberada: {MemoriaLiberada} MB", 
                    _requestStopwatch.ElapsedMilliseconds, memoryFreedMB);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        MemoryBeforeMB = beforeMemoryMB,
                        MemoryAfterMB = afterMemoryMB,
                        MemoryFreedMB = memoryFreedMB,
                        OptimizationTimeMs = _requestStopwatch.ElapsedMilliseconds,
                        Timestamp = DateTime.UtcNow
                    },
                    Message = $"Optimización manual completada - Memoria liberada: {memoryFreedMB} MB",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
            catch (Exception ex)
            {
                _requestStopwatch.Stop();
                _logger.LogError(ex, "❌ Error en optimización manual: {Error}", ex.Message);
                
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error al realizar optimización manual del pool de conexiones",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }
    }
}
```

#### 2.3 DiagnosticoController.cs

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Infrastructure;
using Shared.Security;
using System.Diagnostics;
using Dapper;

namespace GM.CatalogSync.API.Controllers.Diagnostico
{
    [ApiController]
    [Route("api/v1/gm/catalog-sync/diagnostico")]
    [Produces("application/json")]
    [Authorize]
    public class DiagnosticoController : ControllerBase
    {
        private readonly ILogger<DiagnosticoController> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Stopwatch _requestStopwatch;

        public DiagnosticoController(
            ILogger<DiagnosticoController> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _requestStopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Valida la salud de la conexión Oracle
        /// </summary>
        [HttpGet("validar-conexion")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> ValidarConexion()
        {
            var correlationId = CorrelationHelper.GenerateEndpointId("VALIDAR_CONEXION");
            _requestStopwatch.Restart();
            _logger.LogInformation("[{CorrelationId}] Iniciando validación de conexión", correlationId);

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var connectionFactory = scope.ServiceProvider.GetRequiredService<IOracleConnectionFactory>();
                    
                    var validationStopwatch = Stopwatch.StartNew();
                    using var connection = await connectionFactory.CreateConnectionAsync();
                    var result = await connection.QueryFirstOrDefaultAsync<int>("SELECT 1 FROM DUAL");
                    validationStopwatch.Stop();
                    
                    var isValid = result == 1;
                    
                    _requestStopwatch.Stop();
                    
                    if (isValid)
                    {
                        _logger.LogInformation("[{CorrelationId}] Validación de conexión exitosa en {TiempoTotal}ms", 
                            correlationId, _requestStopwatch.ElapsedMilliseconds);
                        
                        return Ok(new ApiResponse<object>
                        {
                            Success = true,
                            Data = new
                            {
                                IsValid = true,
                                ResponseTimeMs = validationStopwatch.ElapsedMilliseconds,
                                Timestamp = DateTime.UtcNow
                            },
                            Message = "Conexión válida",
                            Timestamp = DateTimeHelper.GetMexicoTimeString()
                        });
                    }
                    else
                    {
                        _logger.LogWarning("[{CorrelationId}] Validación de conexión fallida después de {TiempoTotal}ms", 
                            correlationId, _requestStopwatch.ElapsedMilliseconds);
                        
                        return BadRequest(new ApiResponse
                        {
                            Success = false,
                            Message = "Conexión no válida",
                            Timestamp = DateTimeHelper.GetMexicoTimeString()
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _requestStopwatch.Stop();
                _logger.LogError(ex, "[{CorrelationId}] Error crítico en ValidarConexion después de {TiempoTotal}ms: {Error}", 
                    correlationId, _requestStopwatch.ElapsedMilliseconds, ex.Message);
                
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error interno del servidor. Contacte al administrador.",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }

        /// <summary>
        /// Obtiene estadísticas del pool de conexiones
        /// </summary>
        [HttpGet("estadisticas-pool")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> ObtenerEstadisticasPool()
        {
            var correlationId = CorrelationHelper.GenerateEndpointId("ESTADISTICAS_POOL");
            _requestStopwatch.Restart();
            _logger.LogInformation("[{CorrelationId}] Obteniendo estadísticas del pool", correlationId);

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var connectionFactory = scope.ServiceProvider.GetRequiredService<IOracleConnectionFactory>();
                    
                    var statsStopwatch = Stopwatch.StartNew();
                    using var connection = await connectionFactory.CreateConnectionAsync();
                    
                    // Obtener información básica de la conexión
                    var serverVersion = connection.ServerVersion;
                    var databaseName = connection.Database;
                    
                    statsStopwatch.Stop();
                    _requestStopwatch.Stop();
                    
                    _logger.LogInformation("[{CorrelationId}] Estadísticas del pool obtenidas en {TiempoTotal}ms", 
                        correlationId, _requestStopwatch.ElapsedMilliseconds);
                    
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Data = new
                        {
                            ServerVersion = serverVersion,
                            DatabaseName = databaseName,
                            ResponseTimeMs = statsStopwatch.ElapsedMilliseconds,
                            Timestamp = DateTime.UtcNow
                        },
                        Message = "Estadísticas del pool obtenidas exitosamente",
                        Timestamp = DateTimeHelper.GetMexicoTimeString()
                    });
                }
            }
            catch (Exception ex)
            {
                _requestStopwatch.Stop();
                _logger.LogError(ex, "[{CorrelationId}] Error crítico en ObtenerEstadisticasPool después de {TiempoTotal}ms: {Error}", 
                    correlationId, _requestStopwatch.ElapsedMilliseconds, ex.Message);
                
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error interno del servidor. Contacte al administrador.",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }

        /// <summary>
        /// Valida la salud de la conexión (alias de validar-conexion)
        /// </summary>
        [HttpGet("validar-todas-conexiones")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> ValidarTodasLasConexiones()
        {
            var correlationId = CorrelationHelper.GenerateEndpointId("VALIDAR_TODAS_CONEXIONES");
            _requestStopwatch.Restart();
            _logger.LogInformation("[{CorrelationId}] Iniciando validación de todas las conexiones", correlationId);

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var connectionFactory = scope.ServiceProvider.GetRequiredService<IOracleConnectionFactory>();
                    
                    var validationStopwatch = Stopwatch.StartNew();
                    using var connection = await connectionFactory.CreateConnectionAsync();
                    var result = await connection.QueryFirstOrDefaultAsync<int>("SELECT 1 FROM DUAL");
                    validationStopwatch.Stop();
                    
                    var isValid = result == 1;
                    
                    _requestStopwatch.Stop();
                    
                    _logger.LogInformation("[{CorrelationId}] Validación completada en {TiempoTotal}ms. Válida: {IsValid}", 
                        correlationId, _requestStopwatch.ElapsedMilliseconds, isValid);
                    
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Data = new
                        {
                            IsValid = isValid,
                            ResponseTimeMs = validationStopwatch.ElapsedMilliseconds,
                            Timestamp = DateTime.UtcNow
                        },
                        Message = isValid ? "Validación completada. Conexión válida." : "Validación completada. Conexión no válida.",
                        Timestamp = DateTimeHelper.GetMexicoTimeString()
                    });
                }
            }
            catch (Exception ex)
            {
                _requestStopwatch.Stop();
                _logger.LogError(ex, "[{CorrelationId}] Error crítico en ValidarTodasLasConexiones después de {TiempoTotal}ms: {Error}", 
                    correlationId, _requestStopwatch.ElapsedMilliseconds, ex.Message);
                
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error interno del servidor. Contacte al administrador.",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }
    }
}
```

---

### Paso 3: Configurar en Program.cs

Agregar en `GM.CatalogSync.API/Program.cs`:

```csharp
// ⚙️ Registrar servicios en segundo plano para monitoreo y mantenimiento
builder.Services.AddSingleton<Shared.Infrastructure.Services.PerformanceMonitor>();
builder.Services.AddHostedService<Shared.Infrastructure.Services.PerformanceMonitor>(provider => 
    provider.GetRequiredService<Shared.Infrastructure.Services.PerformanceMonitor>());

builder.Services.AddSingleton<Shared.Infrastructure.Services.ConnectionPoolMaintenance>();
builder.Services.AddHostedService<Shared.Infrastructure.Services.ConnectionPoolMaintenance>(provider => 
    provider.GetRequiredService<Shared.Infrastructure.Services.ConnectionPoolMaintenance>());
```

---

## ⏰ Configuración de Timers (Mantener Conexiones Activas)

### 🔥 Ejecución Automática Periódica

**Los timers se ejecutan AUTOMÁTICAMENTE desde que la aplicación inicia.** No necesitas hacer nada manualmente, solo configurarlos una vez.

---

### 📊 PerformanceMonitor

**Este servicio mantiene la aplicación activa y monitorea el rendimiento:**

| Timer | Intervalo | Cuándo se Ejecuta | Qué Hace |
|-------|-----------|-------------------|----------|
| **Keep-Alive** | **Cada 5 minutos** | Automáticamente desde el inicio | Mantiene la aplicación activa, evita cold starts, registra memoria |
| **Health Check** | **Cada 2 minutos** | Automáticamente desde el inicio | Monitorea estado de la aplicación, CPU, memoria, threads |

**Ejemplo de ejecución:**
```
00:00 - Aplicación inicia
00:02 - Health Check #1 (CPU, memoria, threads)
00:05 - Keep-Alive #1 (mantiene aplicación activa)
00:04 - Health Check #2
00:06 - Health Check #3
00:10 - Keep-Alive #2
00:08 - Health Check #4
... y así sucesivamente
```

---

### 🔥 ConnectionPoolMaintenance

**Este servicio mantiene las conexiones Oracle CALIENTES ejecutando queries periódicas:**

| Timer | Intervalo | Inicio Inicial | Cuándo se Ejecuta | Qué Hace |
|-------|-----------|----------------|-------------------|----------|
| **Warm-up** | **Cada 2 minutos** | Después de 30 segundos | Automáticamente | Ejecuta `SELECT 1 FROM DUAL` para mantener conexiones activas y evitar timeouts |
| **Health Check** | **Cada 1 minuto** | Después de 15 segundos | Automáticamente | Verifica salud de las conexiones Oracle |
| **Optimización** | **Cada 3 minutos** | Después de 45 segundos | Automáticamente | Garbage collection y limpieza de memoria |

**Ejemplo de ejecución:**
```
00:00 - Aplicación inicia
00:00:15 - Health Check #1 (verifica conexión Oracle)
00:00:30 - Warm-up #1 (ejecuta SELECT 1 FROM DUAL - mantiene conexión caliente)
00:00:45 - Optimización #1 (garbage collection)
00:01:15 - Health Check #2
00:02:30 - Warm-up #2 (ejecuta SELECT 1 FROM DUAL - mantiene conexión caliente)
00:01:45 - Health Check #3
00:02:45 - Optimización #2
00:03:15 - Health Check #4
00:04:30 - Warm-up #3 (ejecuta SELECT 1 FROM DUAL - mantiene conexión caliente)
... y así sucesivamente
```

**⚠️ IMPORTANTE:** 
- ✅ **Los timers se ejecutan AUTOMÁTICAMENTE** - No necesitas hacer nada manual
- ✅ **El warm-up ejecuta queries periódicas** (`SELECT 1 FROM DUAL`) para mantener las conexiones Oracle activas
- ✅ **Esto evita timeouts** y mantiene el pool de conexiones caliente
- ✅ **Los intervalos son configurables** en el código del servicio

---

### 🔧 Cómo Modificar los Intervalos

Si necesitas cambiar los intervalos, edita el servicio `ConnectionPoolMaintenance.cs`:

```csharp
// Configuración de intervalos (en minutos)
private readonly int _warmupIntervalMinutes = 2;        // Cambiar aquí
private readonly int _healthCheckIntervalMinutes = 1;   // Cambiar aquí
private readonly int _poolOptimizationIntervalMinutes = 3; // Cambiar aquí
```

**Recomendaciones:**
- **Warm-up**: Entre 2-5 minutos (más frecuente = conexiones más calientes, pero más carga)
- **Health Check**: Entre 1-3 minutos (más frecuente = detección más rápida de problemas)
- **Optimización**: Entre 3-10 minutos (más frecuente = más memoria liberada, pero más CPU)

---

## 📊 Endpoints Disponibles

### Performance
- `GET /api/v1/gm/catalog-sync/performance/stats` - Estadísticas de rendimiento
- `GET /api/v1/gm/catalog-sync/performance/health` - Health check
- `POST /api/v1/gm/catalog-sync/performance/optimize` - Optimizar memoria

### ConnectionPool
- `GET /api/v1/gm/catalog-sync/connection-pool/stats` - Estadísticas del pool
- `POST /api/v1/gm/catalog-sync/connection-pool/warmup` - Warm-up manual
- `POST /api/v1/gm/catalog-sync/connection-pool/health-check` - Health check manual
- `POST /api/v1/gm/catalog-sync/connection-pool/optimize` - Optimización manual

### Diagnostico
- `GET /api/v1/gm/catalog-sync/diagnostico/validar-conexion` - Validar conexión
- `GET /api/v1/gm/catalog-sync/diagnostico/estadisticas-pool` - Estadísticas del pool
- `GET /api/v1/gm/catalog-sync/diagnostico/validar-todas-conexiones` - Validar todas las conexiones

---

## ✅ Checklist de Implementación

- [ ] Crear `Shared.Infrastructure/Services/PerformanceMonitor.cs`
- [ ] Crear `Shared.Infrastructure/Services/ConnectionPoolMaintenance.cs`
- [ ] Crear `GM.CatalogSync.API/Controllers/Performance/PerformanceController.cs`
- [ ] Crear `GM.CatalogSync.API/Controllers/ConnectionPool/ConnectionPoolController.cs`
- [ ] Crear `GM.CatalogSync.API/Controllers/Diagnostico/DiagnosticoController.cs`
- [ ] Registrar servicios en `Program.cs`
- [ ] Agregar referencias necesarias (Dapper, Oracle.ManagedDataAccess.Core)
- [ ] Probar endpoints con Swagger
- [ ] Verificar logs para confirmar que los timers se ejecutan

---

## 🔍 Verificación

### Verificar que los servicios se ejecutan:

1. **Revisar logs al iniciar la aplicación:**
   ```
   🚀 PerformanceMonitor iniciado
   🔥 ConnectionPoolMaintenance iniciado
   ```

2. **Revisar logs periódicos:**
   ```
   💓 Keep-Alive: Aplicación activa...
   🔥 WARM-UP DE CONEXIONES ORACLE - CICLO #1
   🏥 Health Check: Uptime: ...
   ```

3. **Probar endpoints manualmente:**
   - `GET /api/v1/gm/catalog-sync/performance/stats`
   - `GET /api/v1/gm/catalog-sync/connection-pool/stats`

---

## 📝 Notas Importantes

1. **Los timers se ejecutan automáticamente** desde que la aplicación inicia
2. **El warm-up mantiene las conexiones activas** ejecutando queries periódicas
3. **Los servicios son Singleton** para mantener estadísticas consistentes
4. **Los timers usan `Timer` de .NET** que es thread-safe
5. **Los servicios implementan `IHostedService`** para integrarse con ASP.NET Core

---

**Última actualización:** 2025-01-16  
**Versión:** 1.0

