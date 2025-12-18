using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;
using System.Diagnostics;

namespace GM.CatalogSync.API.Controllers.ConnectionPool;

/// <summary>
/// Controller para optimizar manualmente el pool de conexiones
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/connection-pool")]
[Produces("application/json")]
[Authorize]
public class PostConnectionPoolOptimizeController : ControllerBase
{
    private readonly ILogger<PostConnectionPoolOptimizeController> _logger;
    private readonly Stopwatch _requestStopwatch;

    public PostConnectionPoolOptimizeController(ILogger<PostConnectionPoolOptimizeController> logger)
    {
        _logger = logger;
        _requestStopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Optimiza manualmente el pool de conexiones
    /// </summary>
    /// <remarks>
    /// Este endpoint fuerza una recolección de basura (garbage collection) para
    /// liberar memoria no utilizada del pool de conexiones.
    /// </remarks>
    /// <returns>Resultado de la optimización</returns>
    [HttpPost("optimize")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
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
                    Timestamp = DateTimeHelper.GetMexicoDateTime()
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

