using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Infrastructure.Services;
using Shared.Security;
using System.Diagnostics;

namespace GM.DealersSincronizacion.API.Controllers.Performance;

/// <summary>
/// Controller para obtener estadísticas de rendimiento de la aplicación
/// </summary>
[ApiController]
[Route("api/v1/gm/dealer-sinc-productos/performance")]
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
    /// Obtiene estadísticas de rendimiento de la aplicación.
    /// </summary>
    /// <remarks>
    /// Este endpoint proporciona información detallada sobre el rendimiento de la aplicación,
    /// incluyendo tiempo de actividad, uso de memoria, CPU, threads y otra información del sistema.
    /// Útil para monitoreo y diagnóstico de rendimiento.
    /// 
    /// **Funcionalidad:**
    /// - Obtiene información del proceso actual (ID, nombre, tiempo de inicio)
    /// - Proporciona estadísticas de rendimiento del monitor de rendimiento (si está disponible)
    /// - Incluye métricas de uso de memoria, CPU y threads
    /// - Muestra información del sistema (máquina, procesadores, versión del SO, versión del framework)
    /// 
    /// **Ejemplo de uso:**
    /// - GET /api/v1/gm/dealer-sinc-productos/performance/stats
    /// 
    /// **Campos en la respuesta:**
    /// - `processId`: ID del proceso actual
    /// - `processName`: Nombre del proceso
    /// - `startTime`: Fecha y hora de inicio del proceso
    /// - `uptime`: Tiempo de actividad del proceso
    /// - `uptimeFormatted`: Tiempo de actividad formateado (dd.hh:mm:ss)
    /// - `memoryUsageMB`: Uso de memoria en MB
    /// - `cpuTime`: Tiempo total de CPU utilizado
    /// - `threadCount`: Número de threads activos
    /// - `machineName`: Nombre de la máquina
    /// - `processorCount`: Número de procesadores disponibles
    /// - `osVersion`: Versión del sistema operativo
    /// - `frameworkVersion`: Versión del framework .NET
    /// - `requestResponseTimeMs`: Tiempo de respuesta de la petición en milisegundos
    /// - `timestamp`: Timestamp de la operación (hora de México)
    /// 
    /// **Validaciones:**
    /// - El usuario debe estar autenticado (JWT requerido)
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Estadísticas completas de rendimiento
    /// - Información del sistema y proceso
    /// - Timestamp de la operación
    /// </remarks>
    /// <returns>Estadísticas de rendimiento de la aplicación</returns>
    /// <response code="200">Operación exitosa. Retorna estadísticas de rendimiento.</response>
    /// <response code="401">No autorizado si no se proporciona un token JWT válido.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
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

