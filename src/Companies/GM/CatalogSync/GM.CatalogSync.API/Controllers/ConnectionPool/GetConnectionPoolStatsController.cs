using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Infrastructure.Services;
using Shared.Security;
using System.Diagnostics;

namespace GM.CatalogSync.API.Controllers.ConnectionPool;

/// <summary>
/// Controller para obtener estadísticas del pool de conexiones
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/connection-pool")]
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
    /// Obtiene estadísticas del pool de conexiones y mantenimiento
    /// </summary>
    /// <remarks>
    /// Este endpoint proporciona información detallada sobre el pool de conexiones Oracle,
    /// incluyendo estadísticas de warm-up, health checks y optimizaciones realizadas.
    /// </remarks>
    /// <returns>Estadísticas del pool de conexiones</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
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

