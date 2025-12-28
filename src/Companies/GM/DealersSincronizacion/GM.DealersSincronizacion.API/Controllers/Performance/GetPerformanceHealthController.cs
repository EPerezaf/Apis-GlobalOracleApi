using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;
using System.Diagnostics;

namespace GM.DealersSincronizacion.API.Controllers.Performance;

/// <summary>
/// Controller para health check básico de la aplicación
/// </summary>
[ApiController]
[Route("api/v1/gm/dealer-sinc/performance")]
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
    /// Endpoint de health check simple de la aplicación.
    /// </summary>
    /// <remarks>
    /// Este endpoint proporciona un health check básico de la aplicación,
    /// incluyendo estado, tiempo de actividad y uso de memoria. Útil para
    /// monitoreo y verificación rápida del estado de la aplicación.
    /// 
    /// **Funcionalidad:**
    /// - Verifica el estado básico de la aplicación (siempre "Healthy" si responde)
    /// - Calcula el tiempo de actividad desde el inicio del proceso
    /// - Obtiene el uso actual de memoria
    /// - Mide el tiempo de respuesta de la petición
    /// 
    /// **Ejemplo de uso:**
    /// - GET /api/v1/gm/dealer-sinc/performance/health
    /// 
    /// **Campos en la respuesta:**
    /// - `status`: Estado de salud de la aplicación (siempre "Healthy" si responde)
    /// - `uptime`: Tiempo de actividad formateado (dd.hh:mm:ss)
    /// - `memoryUsageMB`: Uso de memoria en MB
    /// - `responseTimeMs`: Tiempo de respuesta de la petición en milisegundos
    /// - `timestamp`: Timestamp de la operación (hora de México)
    /// 
    /// **Validaciones:**
    /// - El usuario debe estar autenticado (JWT requerido)
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Estado de salud de la aplicación
    /// - Tiempo de actividad
    /// - Uso de memoria
    /// - Timestamp de la operación
    /// </remarks>
    /// <returns>Estado de salud de la aplicación</returns>
    /// <response code="200">Operación exitosa. Retorna estado de salud de la aplicación.</response>
    /// <response code="401">No autorizado si no se proporciona un token JWT válido.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("health")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
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

