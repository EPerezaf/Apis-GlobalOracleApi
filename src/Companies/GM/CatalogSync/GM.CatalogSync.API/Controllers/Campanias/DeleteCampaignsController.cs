using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;
using System.Diagnostics;

namespace GM.CatalogSync.API.Controllers.Campanias;

/// <summary>
/// Controller para operaciones DELETE de campanias
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/campaign-list-batch-delete")]
[Produces("application/json")]
[Authorize]
[Tags("CampaignList")]
public class DeleteCampaignsController : ControllerBase
{
    private readonly ICampaignService _service;
    private readonly ILogger<DeleteCampaignsController> _logger;

    public DeleteCampaignsController(
        ICampaignService service,
        ILogger<DeleteCampaignsController> logger)
    {
        _service = service;
        _logger = logger;
    }
    /// <summary>
    /// Elimina todas las campanias del listado
    /// </summary>
    /// <remarks>
    /// ⚠️ **ADVERTENCIA:** Este endpoint elimina TODOS los registros de la tabla.
    /// 
    /// **Ejemplo de uso:**
    /// - DELETE /api/v1/gm/catalog-sync/campaign-list-batch-delete
    /// 
    /// **Características técnicas:**
    /// - ✅ Dapper.ExecuteAsync para DELETE
    /// - ✅ Elimina todos los registros sin filtros
    /// - ✅ Invalidación automática de caché
    /// - ✅ Auditoría con usuario JWT
    /// - ✅ No requiere parámetros
    /// 
    /// **Parámetros:**
    /// No requiere parámetros
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Número de registros eliminados
    /// - Timestamp de la operación
    /// </remarks>
    /// <returns>Resultado de la eliminación con cantidad de registros eliminados</returns>
    /// <response code="200">Registros eliminados exitosamente.</response>
    /// <response code="404">No se encontraron registros para eliminar.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> DeleteAllCampaigns()
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var currentUser = JwtUserHelper.GetCurrentUser(User, _logger);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogWarning(
                "Inicio de eliminación de todas las campanias. Usuario: {UserId}, CorrelationId: {CorrelationId}",
                currentUser, correlationId);

            var rowsAffected = await _service.DeleteAllAsync(
                currentUser,
                correlationId);

            stopwatch.Stop();

            if (rowsAffected > 0)
            {
                _logger.LogInformation(
                    "Campanias eliminadas exitosamente. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}, RegistrosEliminados: {RowsAffected}",
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
        catch (CampaignDataAccessException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error de acceso a datos al eliminar campanias. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
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
                "Error inesperado al eliminar campanias. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
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

