using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Infrastructure.Services;
using Shared.Security;
using System.Diagnostics;

namespace GM.CatalogSync.API.Controllers.Performance;

/// <summary>
/// Controller para obtener estadísticas de rendimiento de la aplicación
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/performance")]
[Produces("application/json")]
[Authorize]
public class GetPerformanceStatsController : ControllerBase
{
    private readonly ILogger<GetPerformanceStatsController> _logger;
    private readonly PerformanceMonitor? _performanceMonitor;
    private readonly Stopwatch _requestStopwatch;

    public GetPerformanceStatsController(
        ILogger<GetPerformanceStatsController> logger, 
        PerformanceMonitor? performanceMonitor = null)
    {
        _logger = logger;
        _performanceMonitor = performanceMonitor;
        _requestStopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Obtiene estadísticas de rendimiento de la aplicación
    /// </summary>
    /// <remarks>
    /// Este endpoint proporciona información detallada sobre el rendimiento de la aplicación,
    /// incluyendo tiempo de actividad, uso de memoria, CPU, threads y otra información del sistema.
    /// </remarks>
    /// <returns>Estadísticas de rendimiento de la aplicación</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
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
                Timestamp = DateTimeHelper.GetMexicoDateTime()
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
}

