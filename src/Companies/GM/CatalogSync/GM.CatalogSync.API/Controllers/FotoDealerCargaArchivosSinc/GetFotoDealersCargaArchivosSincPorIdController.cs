using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Exceptions;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.FotoDealersCargaArchivosSinc;

/// <summary>
/// Controller para consulta de foto de dealers carga archivos sincronización por ID.
/// Ruta base: /api/v1/gm/catalog-sync/foto-dealers-carga-archivos-sinc
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/foto-dealers-carga-archivos-sinc")]
[Produces("application/json")]
[Authorize]
public class GetFotoDealersCargaArchivosSincPorIdController : ControllerBase
{
    private readonly IFotoDealersCargaArchivosSincService _service;
    private readonly ILogger<GetFotoDealersCargaArchivosSincPorIdController> _logger;

    public GetFotoDealersCargaArchivosSincPorIdController(
        IFotoDealersCargaArchivosSincService service,
        ILogger<GetFotoDealersCargaArchivosSincPorIdController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene un registro de foto de dealers carga archivos sincronización por su ID
    /// </summary>
    /// <remarks>
    /// Este endpoint permite obtener un registro específico de foto de dealers carga archivos sincronización por su ID único.
    /// 
    /// **Ejemplo de uso:**
    /// - GET /api/v1/gm/catalog-sync/foto-dealers-carga-archivos-sinc/1
    /// 
    /// **Campos en la respuesta:**
    /// - `fotoDealersCargaArchivosSincId`: ID único del registro
    /// - `cargaArchivoSincronizacionId`: ID de la carga de archivo de sincronización
    /// - `idCarga`: ID de la carga (desde CO_CARGAARCHIVOSINCRONIZACION, ej: "products_catalog_16122025_1335")
    /// - `procesoCarga`: Proceso de la carga (desde CO_CARGAARCHIVOSINCRONIZACION, ej: "ProductsCatalog")
    /// - `fechaCarga`: Fecha de carga del archivo (desde CO_CARGAARCHIVOSINCRONIZACION)
    /// - `fechaSincronizacion`: Fecha de sincronización (desde CO_SINCRONIZACIONARCHIVOSDEALERS, puede ser null si no existe registro)
    /// - `tiempoSincronizacionHoras`: Tiempo de sincronización en horas (calculado: FechaSincronizacion - FechaCarga, puede ser null si no existe fechaSincronizacion, ej: 0.97)
    /// - `dealerBac`: Código BAC del dealer
    /// - `nombreDealer`: Nombre comercial del dealer
    /// - `razonSocialDealer`: Razón social legal del dealer
    /// - `dms`: Sistema DMS utilizado
    /// - `fechaRegistro`: Fecha de registro de la fotografía
    /// - Campos de auditoría: fechaAlta, usuarioAlta, fechaModificacion, usuarioModificacion
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Registro de foto de dealers carga archivos sincronización
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="id">ID único del registro de foto de dealers carga archivos sincronización</param>
    /// <returns>Registro de foto de dealers carga archivos sincronización</returns>
    /// <response code="200">Operación exitosa. Retorna el registro solicitado.</response>
    /// <response code="404">Registro no encontrado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<FotoDealersCargaArchivosSincDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "[{CorrelationId}] 🔷 Inicio GET /foto-dealers-carga-archivos-sinc/{Id}",
                correlationId, id);

            var resultado = await _service.ObtenerPorIdAsync(id);

            if (resultado == null)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ GET /foto-dealers-carga-archivos-sinc/{Id} - Registro no encontrado después de {ElapsedMs}ms",
                    correlationId, id, stopwatch.ElapsedMilliseconds);
                return NotFound(new ApiResponse<FotoDealersCargaArchivosSincDto>
                {
                    Success = false,
                    Message = $"No se encontró un registro de foto de dealers carga archivos sincronización con ID {id}",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] ✅ GET /foto-dealers-carga-archivos-sinc/{Id} completado en {ElapsedMs}ms",
                correlationId, id, stopwatch.ElapsedMilliseconds);

            return Ok(new ApiResponse<FotoDealersCargaArchivosSincDto>
            {
                Success = true,
                Message = "Registro obtenido exitosamente",
                Data = resultado,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "[{CorrelationId}] ❌ Error en GET /foto-dealers-carga-archivos-sinc/{Id} después de {ElapsedMs}ms",
                correlationId, id, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error interno del servidor",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}

