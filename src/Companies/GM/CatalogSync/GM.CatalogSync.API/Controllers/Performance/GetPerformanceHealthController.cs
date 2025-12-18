using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;
using System.Diagnostics;

namespace GM.CatalogSync.API.Controllers.Performance;

/// <summary>
/// Controller para health check básico de la aplicación
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/performance")]
[Produces("application/json")]
[Authorize]
public class GetPerformanceHealthController : ControllerBase
{
    private readonly ILogger<GetPerformanceHealthController> _logger;
    private readonly Stopwatch _requestStopwatch;

    public GetPerformanceHealthController(ILogger<GetPerformanceHealthController> logger)
    {
        _logger = logger;
        _requestStopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Endpoint de health check simple
    /// </summary>
    /// <remarks>
    /// Este endpoint proporciona un health check básico de la aplicación,
    /// incluyendo estado, tiempo de actividad y uso de memoria.
    /// </remarks>
    /// <returns>Estado de salud de la aplicación</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public IActionResult HealthCheck()
    {
        _requestStopwatch.Restart();
        
        try
        {
            var process = Process.GetCurrentProcess();
            var memoryMB = process.WorkingSet64 / 1024 / 1024;
            var uptime = DateTimeHelper.GetMexicoDateTime() - process.StartTime;
            
            _requestStopwatch.Stop();
            
            var health = new
            {
                Status = "Healthy",
                Uptime = uptime.ToString(@"dd\.hh\:mm\:ss"),
                MemoryUsageMB = memoryMB,
                ResponseTimeMs = _requestStopwatch.ElapsedMilliseconds,
                Timestamp = DateTimeHelper.GetMexicoDateTime()
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
}

