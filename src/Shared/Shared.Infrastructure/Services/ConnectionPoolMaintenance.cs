using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
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
                _lastWarmupTime = DateTimeHelper.GetMexicoDateTime();
                
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
                _lastHealthCheckTime = DateTimeHelper.GetMexicoDateTime();
                
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
                _lastOptimizationTime = DateTimeHelper.GetMexicoDateTime();
                
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

