using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Exceptions;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.EventoCargaSnapshotDealer;

/// <summary>
/// Controller para consulta de evento de carga snapshot dealer por ID.
/// Ruta base: /api/v1/gm/catalog-sync/evento-carga-snapshot-dealer
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/evento-carga-snapshot-dealer")]
[Produces("application/json")]
[Authorize]
public class GetEventoCargaSnapshotDealerPorIdController : ControllerBase
{
    private readonly IEventoCargaSnapshotDealerService _service;
    private readonly ILogger<GetEventoCargaSnapshotDealerPorIdController> _logger;

    public GetEventoCargaSnapshotDealerPorIdController(
        IEventoCargaSnapshotDealerService service,
        ILogger<GetEventoCargaSnapshotDealerPorIdController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene un registro de evento de carga snapshot dealer por su ID
    /// </summary>
    /// <remarks>
    /// Este endpoint permite obtener un registro específico de evento de carga snapshot dealer por su ID único.
    /// 
    /// **Ejemplo de uso:**
    /// - GET /api/v1/gm/catalog-sync/evento-carga-snapshot-dealer/1
    /// 
    /// **Campos en la respuesta:**
    /// - `eventoCargaSnapshotDealerId`: ID único del registro
    /// - `eventoCargaProcesoId`: ID del evento de carga de proceso
    /// - `idCarga`: ID de la carga (desde CO_EVENTOSCARGAPROCESO, ej: "products_catalog_16122025_1335")
    /// - `procesoCarga`: Proceso de la carga (desde CO_EVENTOSCARGAPROCESO, ej: "ProductsCatalog")
    /// - `fechaCarga`: Fecha de carga del archivo (desde CO_EVENTOSCARGAPROCESO)
    /// - `fechaSincronizacion`: Fecha de sincronización (desde CO_SINCRONIZACIONCARGAPROCESODEALER, puede ser null si no existe registro)
    /// - `tiempoSincronizacionHoras`: Tiempo de sincronización en horas (calculado: FechaSincronizacion - FechaCarga, puede ser null si no existe fechaSincronizacion, ej: 0.97)
    /// - `dealerBac`: Código BAC del dealer
    /// - `nombreDealer`: Nombre comercial del dealer
    /// - `razonSocialDealer`: Razón social legal del dealer
    /// - `dms`: Sistema DMS utilizado
    /// - `fechaRegistro`: Fecha de registro de la fotografía
    /// - `urlWebhook`: URL del webhook del distribuidor (obtenido de CO_DISTRIBUIDORES.CODI_URLWEBHOOK)
    /// - `secretKey`: Secret key del webhook del distribuidor (obtenido de CO_DISTRIBUIDORES.CODI_SECRETKEY)
    /// - Campos de auditoría: fechaAlta, usuarioAlta, fechaModificacion, usuarioModificacion
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Registro de evento de carga snapshot dealer
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="id">ID único del registro de evento de carga snapshot dealer</param>
    /// <returns>Registro de evento de carga snapshot dealer</returns>
    /// <response code="200">Operación exitosa. Retorna el registro solicitado.</response>
    /// <response code="404">Registro no encontrado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EventoCargaSnapshotDealerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "[{CorrelationId}] 🔷 Inicio GET /evento-carga-snapshot-dealer/{Id}",
                correlationId, id);

            var resultado = await _service.ObtenerPorIdAsync(id);

            if (resultado == null)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ GET /evento-carga-snapshot-dealer/{Id} - Registro no encontrado después de {ElapsedMs}ms",
                    correlationId, id, stopwatch.ElapsedMilliseconds);
                return NotFound(new ApiResponse<EventoCargaSnapshotDealerDto>
                {
                    Success = false,
                    Message = $"No se encontró un registro de evento de carga snapshot dealer con ID {id}",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] ✅ GET /evento-carga-snapshot-dealer/{Id} completado en {ElapsedMs}ms",
                correlationId, id, stopwatch.ElapsedMilliseconds);

            return Ok(new ApiResponse<EventoCargaSnapshotDealerDto>
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
                "[{CorrelationId}] ❌ Error en GET /evento-carga-snapshot-dealer/{Id} después de {ElapsedMs}ms",
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

