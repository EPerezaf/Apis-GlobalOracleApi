using System.Diagnostics;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.AsignacionDealers;

[ApiController]
[Route("/api/seguridad/asignaciones/empresaId/userId/dealerId")]
[Produces("application/json")]
[Tags("AsignacionDealer")]
public class DeleteAsignacionDealerController : ControllerBase
{
    private readonly IAsignacionService _service;
    private readonly ILogger<DeleteAsignacionDealerController> _logger;
    public DeleteAsignacionDealerController(
        IAsignacionService service,
        ILogger<DeleteAsignacionDealerController> logger)
    {
        _service =service;
        _logger = logger;    
    }
    
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> EliminarAsignacion(
        [FromQuery] string usuario,
        [FromQuery] string dealer)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var currentUser = JwtUserHelper.GetCurrentUser(User, _logger);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogWarning(
                "Inicio de eliminacion de asignacion por dealer. Usuario: {UserId}, CorrelationId: {CorrelationId}",
                currentUser, correlationId);
            var rowsAffected = await _service.EliminarTodosAsync(
                usuario, dealer,currentUser, correlationId);
            stopwatch.Stop();

            if(rowsAffected > 0)
            {
                _logger.LogInformation(
                    "Asignaciones eliminadas exitosamente. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}, RegistrosEliminados: {RowsAffected}",
                    correlationId, stopwatch.ElapsedMilliseconds, currentUser, rowsAffected);
                
                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = $"Se eliminaron {rowsAffected} registros exitosamente",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
            else
            {
                _logger.LogWarning(
                    "No se encontraron registros para eliminar. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
                    correlationId, stopwatch.ElapsedMilliseconds, currentUser);
                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = "No se encontraron registros para eliminar",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }
        catch (AsignacionDataAccessException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
            "Error de acceso a datos al eliminar asignaciones. CorrelationId: {CorrealtionId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
            correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error al eliminar registros. Por favor intenete nuevamente.",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch(Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error inesperado al eliminar asignaciones. CorrelationId: {CorrealtionId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser);
            
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error interno del servidor. El error ha sido registrado",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}