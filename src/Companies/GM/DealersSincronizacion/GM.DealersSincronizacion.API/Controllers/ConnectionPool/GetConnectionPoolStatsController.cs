using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Infrastructure.Services;
using Shared.Security;
using System.Diagnostics;

namespace GM.DealersSincronizacion.API.Controllers.ConnectionPool;

/// <summary>
/// Controller para obtener estadísticas del pool de conexiones
/// </summary>
[ApiController]
[Route("api/v1/gm/dealer-sinc/connection-pool")]
[Produces("application/json")]
[Authorize]
public class GetConnectionPoolStatsController : ControllerBase
{
    private readonly ILogger<GetConnectionPoolStatsController> _logger;
    private readonly ConnectionPoolMaintenance? _connectionMaintenance;
    private readonly Stopwatch _requestStopwatch;

    public GetConnectionPoolStatsController(
        ILogger<GetConnectionPoolStatsController> logger,
        ConnectionPoolMaintenance? connectionMaintenance = null)
    {
        _logger = logger;
        _connectionMaintenance = connectionMaintenance;
        _requestStopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Obtiene estadísticas del pool de conexiones y mantenimiento.
    /// </summary>
    /// <remarks>
    /// Este endpoint proporciona información detallada sobre el pool de conexiones Oracle,
    /// incluyendo estadísticas de warm-up, health checks y optimizaciones realizadas.
    /// 
    /// **Funcionalidad:**
    /// - Obtiene información del proceso actual (ID, nombre, tiempo de inicio)
    /// - Proporciona estadísticas de mantenimiento del pool de conexiones
    /// - Incluye métricas de uso de memoria
    /// - Muestra tiempos de última ejecución de warm-up, health checks y optimizaciones
    /// 
    /// **Ejemplo de uso:**
    /// - GET /api/v1/gm/dealer-sinc/connection-pool/stats
    /// 
    /// **Campos en la respuesta:**
    /// - `processId`: ID del proceso actual
    /// - `processName`: Nombre del proceso
    /// - `startTime`: Fecha y hora de inicio del proceso
    /// - `uptime`: Tiempo de actividad del proceso
    /// - `uptimeFormatted`: Tiempo de actividad formateado (dd.hh:mm:ss)
    /// - `memoryUsageMB`: Uso de memoria en MB
    /// - `maintenanceStats`: Estadísticas de mantenimiento (si está disponible):
    ///   - `totalWarmupCycles`: Total de ciclos de warm-up ejecutados
    ///   - `totalHealthChecks`: Total de health checks ejecutados
    ///   - `totalOptimizations`: Total de optimizaciones ejecutadas
    ///   - `lastWarmupTime`: Última vez que se ejecutó warm-up
    ///   - `lastHealthCheckTime`: Última vez que se ejecutó health check
    ///   - `lastOptimizationTime`: Última vez que se ejecutó optimización
    ///   - `warmupIntervalMinutes`: Intervalo de warm-up en minutos
    ///   - `healthCheckIntervalMinutes`: Intervalo de health check en minutos
    ///   - `poolOptimizationIntervalMinutes`: Intervalo de optimización en minutos
    /// - `requestResponseTimeMs`: Tiempo de respuesta de la petición en milisegundos
    /// - `timestamp`: Timestamp de la operación (hora de México)
    /// 
    /// **Validaciones:**
    /// - El usuario debe estar autenticado (JWT requerido)
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Estadísticas completas del pool de conexiones
    /// - Información de mantenimiento y métricas
    /// - Timestamp de la operación
    /// </remarks>
    /// <returns>Estadísticas del pool de conexiones y mantenimiento</returns>
    /// <response code="200">Operación exitosa. Retorna estadísticas del pool de conexiones.</response>
    /// <response code="401">No autorizado si no se proporciona un token JWT válido.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
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
                Timestamp = DateTimeHelper.GetMexicoDateTime()
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
}

