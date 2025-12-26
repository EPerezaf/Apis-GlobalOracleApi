using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Infrastructure;
using Shared.Security;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace GM.DealersSincronizacion.API.Controllers.Diagnostico;

/// <summary>
/// Controller para obtener estadísticas del pool de conexiones Oracle
/// </summary>
[ApiController]
[Route("api/v1/gm/dealer-sinc-productos/diagnostico")]
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
    /// Obtiene estadísticas del pool de conexiones Oracle.
    /// </summary>
    /// <remarks>
    /// Este endpoint proporciona información sobre el pool de conexiones Oracle,
    /// incluyendo versión del servidor y nombre de la base de datos. Útil para
    /// diagnóstico y monitoreo de la conexión a la base de datos.
    /// 
    /// **Funcionalidad:**
    /// - Obtiene la versión del servidor Oracle
    /// - Obtiene el nombre de la base de datos
    /// - Mide el tiempo de respuesta de la consulta
    /// - Proporciona información de diagnóstico de la conexión
    /// 
    /// **Ejemplo de uso:**
    /// - GET /api/v1/gm/dealer-sinc-productos/diagnostico/estadisticas-pool
    /// 
    /// **Campos en la respuesta:**
    /// - `serverVersion`: Versión del servidor Oracle (ej: "21.0.0.0.0")
    /// - `databaseName`: Nombre de la base de datos Oracle (ej: "AUTOS")
    /// - `responseTimeMs`: Tiempo de respuesta de la consulta en milisegundos
    /// - `timestamp`: Timestamp de la operación (hora de México)
    /// 
    /// **Validaciones:**
    /// - El usuario debe estar autenticado (JWT requerido)
    /// - La conexión Oracle debe estar disponible
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Información del servidor Oracle
    /// - Nombre de la base de datos
    /// - Tiempo de respuesta
    /// - Timestamp de la operación
    /// </remarks>
    /// <returns>Estadísticas del pool de conexiones Oracle</returns>
    /// <response code="200">Operación exitosa. Retorna estadísticas del pool de conexiones.</response>
    /// <response code="401">No autorizado si no se proporciona un token JWT válido.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("estadisticas-pool")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
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

