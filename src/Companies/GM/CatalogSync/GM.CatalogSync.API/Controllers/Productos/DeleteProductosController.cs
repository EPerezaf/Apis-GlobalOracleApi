using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;
using System.Diagnostics;

namespace GM.CatalogSync.API.Controllers.Productos;

/// <summary>
/// Controller para operaciones DELETE de productos
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/productos")]
[Produces("application/json")]
[Authorize]
public class DeleteProductosController : ControllerBase
{
    private readonly IProductoService _service;
    private readonly ILogger<DeleteProductosController> _logger;

    public DeleteProductosController(
        IProductoService service,
        ILogger<DeleteProductosController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Elimina todos los productos del listado
    /// </summary>
    /// <remarks>
    /// ⚠️ **ADVERTENCIA:** Este endpoint elimina TODOS los registros de la tabla.
    /// 
    /// **Ejemplo de uso:**
    /// - DELETE /api/v1/gm/catalog-sync/productos
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
    public async Task<IActionResult> EliminarTodosProductos()
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var currentUser = JwtUserHelper.GetCurrentUser(User, _logger);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogWarning(
                "Inicio de eliminación de todos los productos. Usuario: {UserId}, CorrelationId: {CorrelationId}",
                currentUser, correlationId);

            var rowsAffected = await _service.EliminarTodosAsync(
                currentUser,
                correlationId);

            stopwatch.Stop();

            if (rowsAffected > 0)
            {
                _logger.LogInformation(
                    "Productos eliminados exitosamente. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}, RegistrosEliminados: {RowsAffected}",
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
        catch (ProductoDataAccessException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error de acceso a datos al eliminar productos. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
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
                "Error inesperado al eliminar productos. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
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

