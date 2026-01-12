using System.Diagnostics;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.Campañas;

[ApiController]
[Route("api/v1/gm/catalog-sync/campaign-list-batch-delete")]
[Produces("application/json")]
[Tags("CampaignList")]
public class DeleteCampañasController : ControllerBase
{
    private readonly ICampañaService _service;
    private readonly ILogger<DeleteCampañasController> _logger;
    public DeleteCampañasController( 
        ICampañaService service,
        ILogger<DeleteCampañasController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> EliminarTodasCampañas()
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var currentUser = JwtUserHelper.GetCurrentUser(User, _logger);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogWarning(
                "Inicio de eliminacion de todos los productos. Usuario: {User} , CorrelationId: {CorrelationId}",
                currentUser, correlationId);

            var rowsAffected = await _service.EliminarTodosAsync(
                currentUser, correlationId);
            
            stopwatch.Stop();

            if(rowsAffected > 0)
            {
                _logger.LogInformation(
                    "Campañas eliminadas exitosamente. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}, RegistrosEliminados: {RowsAffected}",
                    correlationId, stopwatch.ElapsedMilliseconds, currentUser, rowsAffected);
                
                return Ok(new ApiResponse
                {
                    Success = true,
                    Message = $"Se eliminaron {rowsAffected} regsitros exitosamente",
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
        catch (CampañaDataAccessException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Error de acceso a datos al eliminar campañas. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
                    correlationId, stopwatch.ElapsedMilliseconds, currentUser);

                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error al eliminar registros. Por favor, intente nuevamente.",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Error inesperado al eliminar campañas. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
                    correlationId, stopwatch.ElapsedMilliseconds, currentUser);

                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error interno del servidor. El error ha sido registrado.",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
    }
}