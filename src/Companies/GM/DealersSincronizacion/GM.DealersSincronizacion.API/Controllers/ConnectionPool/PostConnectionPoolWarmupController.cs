using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Infrastructure;
using Shared.Security;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace GM.DealersSincronizacion.API.Controllers.ConnectionPool;

/// <summary>
/// Controller para realizar warm-up manual del pool de conexiones
/// </summary>
[ApiController]
[Route("api/v1/gm/dealer-sinc/connection-pool")]
[Produces("application/json")]
[Authorize]
public class PostConnectionPoolWarmupController : ControllerBase
{
    private readonly ILogger<PostConnectionPoolWarmupController> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Stopwatch _requestStopwatch;

    public PostConnectionPoolWarmupController(
        ILogger<PostConnectionPoolWarmupController> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _requestStopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Realiza un warm-up manual del pool de conexiones.
    /// </summary>
    /// <remarks>
    /// Este endpoint ejecuta una consulta simple (SELECT 1 FROM DUAL) para mantener
    /// las conexiones Oracle activas y evitar timeouts. Útil cuando el pool de conexiones
    /// ha estado inactivo por un período prolongado.
    /// 
    /// **Funcionalidad:**
    /// - Ejecuta una consulta de validación simple contra Oracle
    /// - Mantiene las conexiones del pool activas
    /// - Previene timeouts de conexión inactiva
    /// - Mide el tiempo de respuesta de la consulta
    /// 
    /// **Ejemplo de uso:**
    /// - POST /api/v1/gm/dealer-sinc/connection-pool/warmup
    /// 
    /// **Campos en la respuesta:**
    /// - `success`: Indica si el warm-up fue exitoso (true si la consulta retornó 1)
    /// - `responseTimeMs`: Tiempo de respuesta de la consulta en milisegundos
    /// - `timestamp`: Timestamp de la operación (hora de México)
    /// 
    /// **Validaciones:**
    /// - El usuario debe estar autenticado (JWT requerido)
    /// - La conexión Oracle debe estar disponible
    /// - Si la consulta falla, retorna success=false
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Estado del warm-up (exitoso o fallido)
    /// - Tiempo de respuesta de la consulta
    /// - Timestamp de la operación
    /// </remarks>
    /// <returns>Resultado del warm-up manual del pool de conexiones</returns>
    /// <response code="200">Operación completada. Retorna resultado del warm-up.</response>
    /// <response code="401">No autorizado si no se proporciona un token JWT válido.</response>
    /// <response code="500">Error interno del servidor al realizar warm-up.</response>
    [HttpPost("warmup")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> PerformManualWarmup()
    {
        _requestStopwatch.Restart();
        _logger.LogInformation("🔥 Iniciando warm-up manual del pool de conexiones");

        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var connectionFactory = scope.ServiceProvider.GetRequiredService<IOracleConnectionFactory>();
                
                var warmupStopwatch = Stopwatch.StartNew();
                using var connection = await connectionFactory.CreateConnectionAsync();
                
                var result = await connection.QueryFirstOrDefaultAsync<int>("SELECT 1 FROM DUAL");
                warmupStopwatch.Stop();
                
                _requestStopwatch.Stop();
                
                _logger.LogInformation("🔥 Warm-up manual completado en {TiempoWarmup}ms", 
                    _requestStopwatch.ElapsedMilliseconds);

                return Ok(new ApiResponse<object>
                {
                    Success = result == 1,
                    Data = new
                    {
                        Success = result == 1,
                        ResponseTimeMs = warmupStopwatch.ElapsedMilliseconds,
                        Timestamp = DateTimeHelper.GetMexicoDateTime()
                    },
                    Message = result == 1 ? "Warm-up manual completado exitosamente" : "Warm-up falló",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }
        catch (Exception ex)
        {
            _requestStopwatch.Stop();
            _logger.LogError(ex, "❌ Error en warm-up manual: {Error}", ex.Message);
            
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error al realizar warm-up manual del pool de conexiones",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}

