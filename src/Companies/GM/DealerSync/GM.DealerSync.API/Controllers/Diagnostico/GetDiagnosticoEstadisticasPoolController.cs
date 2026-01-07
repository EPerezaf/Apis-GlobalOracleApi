using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Infrastructure;
using Shared.Security;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace GM.DealerSync.API.Controllers.Diagnostico;

/// <summary>
/// Controller para obtener estadísticas del pool de conexiones Oracle
/// </summary>
[ApiController]
[Route("api/v1/gm/dealer-sync/diagnostico")]
[Produces("application/json")]
[Authorize]
public class GetDiagnosticoEstadisticasPoolController : ControllerBase
{
    private readonly ILogger<GetDiagnosticoEstadisticasPoolController> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Stopwatch _requestStopwatch;

    public GetDiagnosticoEstadisticasPoolController(
        ILogger<GetDiagnosticoEstadisticasPoolController> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _requestStopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Obtiene estadísticas del pool de conexiones
    /// </summary>
    /// <remarks>
    /// Este endpoint proporciona información sobre el pool de conexiones Oracle,
    /// incluyendo versión del servidor y nombre de la base de datos.
    /// </remarks>
    /// <returns>Estadísticas del pool de conexiones</returns>
    [HttpGet("estadisticas-pool")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> ObtenerEstadisticasPool()
    {
        var correlationId = CorrelationHelper.GenerateEndpointId("ESTADISTICAS_POOL");
        _requestStopwatch.Restart();
        _logger.LogInformation("[{CorrelationId}] Obteniendo estadísticas del pool", correlationId);

        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var connectionFactory = scope.ServiceProvider.GetRequiredService<IOracleConnectionFactory>();
                
                var statsStopwatch = Stopwatch.StartNew();
                using var connection = await connectionFactory.CreateConnectionAsync();
                
                // Obtener información básica de la conexión
                var serverVersion = connection.ServerVersion;
                var databaseName = connection.Database;
                
                statsStopwatch.Stop();
                _requestStopwatch.Stop();
                
                _logger.LogInformation("[{CorrelationId}] Estadísticas del pool obtenidas en {TiempoTotal}ms", 
                    correlationId, _requestStopwatch.ElapsedMilliseconds);
                
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        ServerVersion = serverVersion,
                        DatabaseName = databaseName,
                        ResponseTimeMs = statsStopwatch.ElapsedMilliseconds,
                        Timestamp = DateTimeHelper.GetMexicoDateTime()
                    },
                    Message = "Estadísticas del pool obtenidas exitosamente",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }
        catch (Exception ex)
        {
            _requestStopwatch.Stop();
            _logger.LogError(ex, "[{CorrelationId}] Error crítico en ObtenerEstadisticasPool después de {TiempoTotal}ms: {Error}", 
                correlationId, _requestStopwatch.ElapsedMilliseconds, ex.Message);
            
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error interno del servidor. Contacte al administrador.",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}

