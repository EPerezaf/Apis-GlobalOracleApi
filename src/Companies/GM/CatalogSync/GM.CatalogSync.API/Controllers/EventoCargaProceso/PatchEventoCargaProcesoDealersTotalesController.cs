using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Exceptions;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.EventoCargaProceso;

/// <summary>
/// Controller para actualizar DealersTotales de Evento de Carga de Proceso.
/// Ruta base: /api/v1/gm/catalog-sync/evento-carga-proceso
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/evento-carga-proceso")]
[Produces("application/json")]
[Authorize]
public class PatchEventoCargaProcesoDealersTotalesController : ControllerBase
{
    private readonly IEventoCargaProcesoService _service;
    private readonly ILogger<PatchEventoCargaProcesoDealersTotalesController> _logger;

    public PatchEventoCargaProcesoDealersTotalesController(
        IEventoCargaProcesoService service,
        ILogger<PatchEventoCargaProcesoDealersTotalesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Actualiza el valor de DealersTotales basado en el conteo de dealers únicos en EventoCargaSnapshotDealer
    /// </summary>
    /// <remarks>
    /// Este endpoint actualiza el campo `dealersTotales` de un registro de evento de carga de proceso
    /// basándose en el conteo de dealers únicos (DISTINCT) que existen en la tabla `CO_EVENTOSCARGASNAPSHOTDEALERS`
    /// para el `eventoCargaProcesoId` especificado.
    /// 
    /// **Ejemplo de uso:**
    /// - PATCH /api/v1/gm/catalog-sync/evento-carga-proceso/1/dealers-totales
    /// 
    /// **Lógica de actualización:**
    /// - Cuenta los dealers únicos (DISTINCT COSD_DEALERBAC) en `CO_EVENTOSCARGASNAPSHOTDEALERS`
    /// - Actualiza `COCP_DEALERSTOTALES` con ese conteo
    /// - Actualiza `COCP_FECHAMODIFICACION` y `COCP_USUARIOMODIFICACION` automáticamente
    /// 
    /// **Campos en la respuesta:**
    /// - Todos los campos del registro de evento de carga actualizado, incluyendo el nuevo valor de `dealersTotales`
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Registro de evento de carga actualizado
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="eventoCargaProcesoId">ID del registro de evento de carga de proceso</param>
    /// <returns>Registro de evento de carga actualizado con el nuevo valor de DealersTotales</returns>
    /// <response code="200">Operación exitosa. Retorna el registro actualizado.</response>
    /// <response code="404">Registro no encontrado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPatch("{eventoCargaProcesoId}/dealers-totales")]
    [ProducesResponseType(typeof(ApiResponse<EventoCargaProcesoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ActualizarDealersTotales(int eventoCargaProcesoId)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var stopwatch = Stopwatch.StartNew();
        var usuarioModificacion = JwtUserHelper.GetCurrentUser(User, _logger);

        try
        {
            _logger.LogInformation(
                "[{CorrelationId}] 🔷 Inicio PATCH /evento-carga-proceso/{EventoCargaProcesoId}/dealers-totales. Usuario: {Usuario}",
                correlationId, eventoCargaProcesoId, usuarioModificacion);

            var resultado = await _service.ActualizarDealersTotalesAsync(
                eventoCargaProcesoId,
                usuarioModificacion);

            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] ✅ PATCH /evento-carga-proceso/{EventoCargaProcesoId}/dealers-totales completado en {ElapsedMs}ms. DealersTotales actualizado: {DealersTotales}",
                correlationId, eventoCargaProcesoId, stopwatch.ElapsedMilliseconds, resultado.DealersTotales);

            return Ok(new ApiResponse<EventoCargaProcesoDto>
            {
                Success = true,
                Message = $"DealersTotales actualizado exitosamente. Nuevo valor: {resultado.DealersTotales}",
                Data = resultado,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (NotFoundException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "[{CorrelationId}] ⚠️ PATCH /evento-carga-proceso/{EventoCargaProcesoId}/dealers-totales - Registro no encontrado después de {ElapsedMs}ms",
                correlationId, eventoCargaProcesoId, stopwatch.ElapsedMilliseconds);
            return NotFound(new ApiResponse<EventoCargaProcesoDto>
            {
                Success = false,
                Message = ex.Message,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "[{CorrelationId}] ❌ Error en PATCH /evento-carga-proceso/{EventoCargaProcesoId}/dealers-totales después de {ElapsedMs}ms",
                correlationId, eventoCargaProcesoId, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error interno del servidor",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}

