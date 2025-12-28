using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Infrastructure;
using Shared.Security;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace GM.DealersSincronizacion.API.Controllers.Diagnostico;

/// <summary>
/// Controller para validar todas las conexiones Oracle
/// </summary>
[ApiController]
[Route("api/v1/gm/dealer-sinc/diagnostico")]
[Produces("application/json")]
[Authorize]
public class GetDiagnosticoValidarTodasConexionesController : ControllerBase
{
    private readonly ILogger<GetDiagnosticoValidarTodasConexionesController> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Stopwatch _requestStopwatch;

    public GetDiagnosticoValidarTodasConexionesController(
        ILogger<GetDiagnosticoValidarTodasConexionesController> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _requestStopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Valida la salud de todas las conexiones Oracle.
    /// </summary>
    /// <remarks>
    /// Este endpoint valida que todas las conexiones Oracle estén funcionando correctamente
    /// ejecutando una consulta de validación (SELECT 1 FROM DUAL). Similar a `validar-conexion`
    /// pero con un nombre más descriptivo para validación de todas las conexiones del pool.
    /// 
    /// **Funcionalidad:**
    /// - Ejecuta una consulta de validación simple contra Oracle
    /// - Verifica que las conexiones del pool estén funcionando correctamente
    /// - Mide el tiempo de respuesta de la consulta
    /// - Retorna el estado de validación (válida o no válida)
    /// 
    /// **Ejemplo de uso:**
    /// - GET /api/v1/gm/dealer-sinc/diagnostico/validar-todas-conexiones
    /// 
    /// **Campos en la respuesta:**
    /// - `isValid`: Indica si las conexiones son válidas (true si la consulta retornó 1)
    /// - `responseTimeMs`: Tiempo de respuesta de la consulta en milisegundos
    /// - `timestamp`: Timestamp de la operación (hora de México)
    /// 
    /// **Validaciones:**
    /// - El usuario debe estar autenticado (JWT requerido)
    /// - Las conexiones Oracle deben estar disponibles
    /// - Si la consulta falla, retorna isValid=false
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Estado de validación de las conexiones
    /// - Tiempo de respuesta de la consulta
    /// - Timestamp de la operación
    /// </remarks>
    /// <returns>Estado de validación de todas las conexiones Oracle</returns>
    /// <response code="200">Operación exitosa. Retorna estado de validación de las conexiones.</response>
    /// <response code="401">No autorizado si no se proporciona un token JWT válido.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("validar-todas-conexiones")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> ValidarTodasLasConexiones()
    {
        var correlationId = CorrelationHelper.GenerateEndpointId("VALIDAR_TODAS_CONEXIONES");
        _requestStopwatch.Restart();
        _logger.LogInformation("[{CorrelationId}] Iniciando validación de todas las conexiones", correlationId);

        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var connectionFactory = scope.ServiceProvider.GetRequiredService<IOracleConnectionFactory>();
                
                var validationStopwatch = Stopwatch.StartNew();
                using var connection = await connectionFactory.CreateConnectionAsync();
                var result = await connection.QueryFirstOrDefaultAsync<int>("SELECT 1 FROM DUAL");
                validationStopwatch.Stop();
                
                var isValid = result == 1;
                
                _requestStopwatch.Stop();
                
                _logger.LogInformation("[{CorrelationId}] Validación completada en {TiempoTotal}ms. Válida: {IsValid}", 
                    correlationId, _requestStopwatch.ElapsedMilliseconds, isValid);
                
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        IsValid = isValid,
                        ResponseTimeMs = validationStopwatch.ElapsedMilliseconds,
                        Timestamp = DateTimeHelper.GetMexicoDateTime()
                    },
                    Message = isValid ? "Validación completada. Conexión válida." : "Validación completada. Conexión no válida.",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }
        catch (Exception ex)
        {
            _requestStopwatch.Stop();
            _logger.LogError(ex, "[{CorrelationId}] Error crítico en ValidarTodasLasConexiones después de {TiempoTotal}ms: {Error}", 
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

