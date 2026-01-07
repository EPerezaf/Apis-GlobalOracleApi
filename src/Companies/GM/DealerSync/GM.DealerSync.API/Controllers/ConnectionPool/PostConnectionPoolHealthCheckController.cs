using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Infrastructure;
using Shared.Security;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace GM.DealerSync.API.Controllers.ConnectionPool;

/// <summary>
/// Controller para realizar health check manual del pool de conexiones
/// </summary>
[ApiController]
[Route("api/v1/gm/dealer-sync/connection-pool")]
[Produces("application/json")]
[Authorize]
public class PostConnectionPoolHealthCheckController : ControllerBase
{
    private readonly ILogger<PostConnectionPoolHealthCheckController> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly Stopwatch _requestStopwatch;

    public PostConnectionPoolHealthCheckController(
        ILogger<PostConnectionPoolHealthCheckController> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _requestStopwatch = Stopwatch.StartNew();
    }

    /// <summary>
    /// Realiza un health check manual del pool de conexiones
    /// </summary>
    /// <remarks>
    /// Este endpoint verifica la salud de las conexiones Oracle ejecutando
    /// una consulta de validación.
    /// </remarks>
    /// <returns>Estado de salud del pool de conexiones</returns>
    [HttpPost("health-check")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> PerformManualHealthCheck()
    {
        _requestStopwatch.Restart();
        _logger.LogInformation("🏥 Iniciando health check manual del pool de conexiones");

        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var connectionFactory = scope.ServiceProvider.GetRequiredService<IOracleConnectionFactory>();
                
                var healthStopwatch = Stopwatch.StartNew();
                using var connection = await connectionFactory.CreateConnectionAsync();
                var result = await connection.QueryFirstOrDefaultAsync<int>("SELECT 1 FROM DUAL");
                healthStopwatch.Stop();
                
                var isHealthy = result == 1;
                
                _requestStopwatch.Stop();
                
                _logger.LogInformation("🏥 Health check manual completado en {TiempoHealthCheck}ms - Status: {Status}", 
                    _requestStopwatch.ElapsedMilliseconds, isHealthy ? "Healthy" : "Unhealthy");

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new
                    {
                        IsHealthy = isHealthy,
                        Status = isHealthy ? "Healthy" : "Unhealthy",
                        ResponseTimeMs = healthStopwatch.ElapsedMilliseconds,
                        Timestamp = DateTimeHelper.GetMexicoDateTime()
                    },
                    Message = isHealthy ? "Health check completado - Conexión saludable" : "Health check completado - Conexión no saludable",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }
        catch (Exception ex)
        {
            _requestStopwatch.Stop();
            _logger.LogError(ex, "❌ Error en health check manual: {Error}", ex.Message);
            
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error al realizar health check manual del pool de conexiones",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}

