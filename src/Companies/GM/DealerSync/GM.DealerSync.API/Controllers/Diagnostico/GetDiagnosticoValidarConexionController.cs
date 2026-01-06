using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Infrastructure;
using Shared.Security;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace GM.DealerSync.API.Controllers.Diagnostico;

/// <summary>
/// Controller para validar la salud de la conexión Oracle
/// </summary>
[ApiController]
[Route("api/v1/gm/dealer-sync/diagnostico")]
[Produces("application/json")]
[Authorize]
public class GetDiagnosticoValidarConexionController : ControllerBase
{
    private readonly ILogger<GetDiagnosticoValidarConexionController> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Stopwatch _requestStopwatch;

    public GetDiagnosticoValidarConexionController(
        ILogger<GetDiagnosticoValidarConexionController> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _requestStopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Valida la salud de la conexión Oracle
    /// </summary>
    /// <remarks>
    /// Este endpoint valida que la conexión Oracle esté funcionando correctamente
    /// ejecutando una consulta de validación.
    /// </remarks>
    /// <returns>Estado de validación de la conexión</returns>
    [HttpGet("validar-conexion")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> ValidarConexion()
    {
        var correlationId = CorrelationHelper.GenerateEndpointId("VALIDAR_CONEXION");
        _requestStopwatch.Restart();
        _logger.LogInformation("[{CorrelationId}] Iniciando validación de conexión", correlationId);

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
                
                if (isValid)
                {
                    _logger.LogInformation("[{CorrelationId}] Validación de conexión exitosa en {TiempoTotal}ms", 
                        correlationId, _requestStopwatch.ElapsedMilliseconds);
                    
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Data = new
                        {
                            IsValid = true,
                            ResponseTimeMs = validationStopwatch.ElapsedMilliseconds,
                            Timestamp = DateTimeHelper.GetMexicoDateTime()
                        },
                        Message = "Conexión válida",
                        Timestamp = DateTimeHelper.GetMexicoTimeString()
                    });
                }
                else
                {
                    _logger.LogWarning("[{CorrelationId}] Validación de conexión fallida después de {TiempoTotal}ms", 
                        correlationId, _requestStopwatch.ElapsedMilliseconds);
                    
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Conexión no válida",
                        Timestamp = DateTimeHelper.GetMexicoTimeString()
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _requestStopwatch.Stop();
            _logger.LogError(ex, "[{CorrelationId}] Error crítico en ValidarConexion después de {TiempoTotal}ms: {Error}", 
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

