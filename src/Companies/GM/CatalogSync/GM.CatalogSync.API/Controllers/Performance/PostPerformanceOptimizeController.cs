using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;
using System.Diagnostics;

namespace GM.CatalogSync.API.Controllers.Performance;

/// <summary>
/// Controller para optimizar la memoria de la aplicación
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/performance")]
[Produces("application/json")]
[Authorize]
public class PostPerformanceOptimizeController : ControllerBase
{
    private readonly ILogger<PostPerformanceOptimizeController> _logger;
    private readonly Stopwatch _requestStopwatch;

    public PostPerformanceOptimizeController(ILogger<PostPerformanceOptimizeController> logger)
    {
        _logger = logger;
        _requestStopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Endpoint para forzar garbage collection y optimizar memoria
    /// </summary>
    /// <remarks>
    /// Este endpoint fuerza una recolección de basura (garbage collection) para liberar memoria no utilizada.
    /// Útil cuando la aplicación ha estado ejecutándose por mucho tiempo y necesita liberar memoria.
    /// </remarks>
    /// <returns>Resultado de la optimización de memoria</returns>
    [HttpPost("optimize")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
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
                            Timestamp = DateTimeHelper.GetMexicoDateTime()
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

