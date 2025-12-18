using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Security;
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
            
            _logger.LogInformation("🚀 PerformanceMonitor iniciado - Tiempo de inicio: {TiempoInicio}", DateTimeHelper.GetMexicoDateTime());
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

