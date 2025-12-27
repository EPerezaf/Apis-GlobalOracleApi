using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Exceptions;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.CargaArchivoSinc;

/// <summary>
/// Controller para actualizar DealersTotales de Carga de Archivo de Sincronización.
/// Ruta base: /api/v1/gm/catalog-sync/carga-archivos-sinc
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/carga-archivos-sinc")]
[Produces("application/json")]
[Authorize]
public class PatchCargaArchivosSincDealersTotalesController : ControllerBase
{
    private readonly ICargaArchivoSincService _service;
    private readonly ILogger<PatchCargaArchivosSincDealersTotalesController> _logger;

    public PatchCargaArchivosSincDealersTotalesController(
        ICargaArchivoSincService service,
        ILogger<PatchCargaArchivosSincDealersTotalesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Actualiza el valor de DealersTotales basado en el conteo de dealers únicos en FotoDealersCargaArchivosSinc
    /// </summary>
    /// <remarks>
    /// Este endpoint actualiza el campo `dealersTotales` de un registro de carga de archivo de sincronización
    /// basándose en el conteo de dealers únicos (DISTINCT) que existen en la tabla `CO_FOTODEALERSCARGAARCHIVOSSINC`
    /// para el `cargaArchivoSincronizacionId` especificado.
    /// 
    /// **Ejemplo de uso:**
    /// - PATCH /api/v1/gm/catalog-sync/carga-archivos-sinc/1/dealers-totales
    /// 
    /// **Lógica de actualización:**
    /// - Cuenta los dealers únicos (DISTINCT COSA_DEALERBAC) en `CO_FOTODEALERSCARGAARCHIVOSSINC`
    /// - Actualiza `COCA_DEALERSTOTALES` con ese conteo
    /// - Actualiza `FECHAMODIFICACION` y `USUARIOMODIFICACION` automáticamente
    /// 
    /// **Campos en la respuesta:**
    /// - Todos los campos del registro de carga actualizado, incluyendo el nuevo valor de `dealersTotales`
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Registro de carga actualizado
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="cargaArchivoSincronizacionId">ID del registro de carga de archivo de sincronización</param>
    /// <returns>Registro de carga actualizado con el nuevo valor de DealersTotales</returns>
    /// <response code="200">Operación exitosa. Retorna el registro actualizado.</response>
    /// <response code="404">Registro no encontrado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPatch("{cargaArchivoSincronizacionId}/dealers-totales")]
    [ProducesResponseType(typeof(ApiResponse<CargaArchivoSincDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ActualizarDealersTotales(int cargaArchivoSincronizacionId)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var stopwatch = Stopwatch.StartNew();
        var usuarioModificacion = JwtUserHelper.GetCurrentUser(User, _logger);

        try
        {
            _logger.LogInformation(
                "[{CorrelationId}] 🔷 Inicio PATCH /carga-archivos-sinc/{CargaArchivoSincronizacionId}/dealers-totales. Usuario: {Usuario}",
                correlationId, cargaArchivoSincronizacionId, usuarioModificacion);

            var resultado = await _service.ActualizarDealersTotalesAsync(
                cargaArchivoSincronizacionId,
                usuarioModificacion);

            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] ✅ PATCH /carga-archivos-sinc/{CargaArchivoSincronizacionId}/dealers-totales completado en {ElapsedMs}ms. DealersTotales actualizado: {DealersTotales}",
                correlationId, cargaArchivoSincronizacionId, stopwatch.ElapsedMilliseconds, resultado.DealersTotales);

            return Ok(new ApiResponse<CargaArchivoSincDto>
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
                "[{CorrelationId}] ⚠️ PATCH /carga-archivos-sinc/{CargaArchivoSincronizacionId}/dealers-totales - Registro no encontrado después de {ElapsedMs}ms",
                correlationId, cargaArchivoSincronizacionId, stopwatch.ElapsedMilliseconds);
            return NotFound(new ApiResponse<CargaArchivoSincDto>
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
                "[{CorrelationId}] ❌ Error en PATCH /carga-archivos-sinc/{CargaArchivoSincronizacionId}/dealers-totales después de {ElapsedMs}ms",
                correlationId, cargaArchivoSincronizacionId, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error interno del servidor",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}

