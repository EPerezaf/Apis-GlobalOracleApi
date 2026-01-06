using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.EventoCargaSnapshotDealer;

/// <summary>
/// Controller para consulta de eventos de carga snapshot de dealers.
/// Ruta base: /api/v1/gm/catalog-sync/evento-carga-snapshot-dealer
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/evento-carga-snapshot-dealer")]
[Produces("application/json")]
[Authorize]
public class GetEventoCargaSnapshotDealerController : ControllerBase
{
    private readonly IEventoCargaSnapshotDealerService _service;
    private readonly ILogger<GetEventoCargaSnapshotDealerController> _logger;

    public GetEventoCargaSnapshotDealerController(
        IEventoCargaSnapshotDealerService service,
        ILogger<GetEventoCargaSnapshotDealerController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los registros de eventos de carga snapshot de dealers con filtros opcionales
    /// </summary>
    /// <remarks>
    /// Este endpoint permite obtener el listado de eventos de carga snapshot de dealers con filtros opcionales.
    /// 
    /// **Parámetros opcionales:**
    /// - `eventoCargaProcesoId`: Filtrar por ID de evento de carga de proceso
    /// - `dealerBac`: Filtrar por código BAC del dealer (búsqueda parcial)
    /// - `dms`: Filtrar por sistema DMS (búsqueda parcial)
    /// - `sincronizado`: Filtrar por estado de sincronización (0 = no sincronizado, 1 = sincronizado). Si no se envía, retorna todos.
    /// 
    /// **Ejemplos de uso:**
    /// - GET /api/v1/gm/catalog-sync/evento-carga-snapshot-dealer
    /// - GET /api/v1/gm/catalog-sync/evento-carga-snapshot-dealer?eventoCargaProcesoId=1
    /// - GET /api/v1/gm/catalog-sync/evento-carga-snapshot-dealer?dealerBac=DEALER001
    /// - GET /api/v1/gm/catalog-sync/evento-carga-snapshot-dealer?dms=CDK
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
    /// - `fechaRegistro`: Fecha de registro del snapshot
    /// - `urlWebhook`: URL del webhook del distribuidor (obtenido de CO_DISTRIBUIDORES.CODI_URLWEBHOOK)
    /// - `secretKey`: Secret key del webhook del distribuidor (obtenido de CO_DISTRIBUIDORES.CODI_SECRETKEY)
    /// - Campos de auditoría: fechaAlta, usuarioAlta, fechaModificacion, usuarioModificacion
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Lista de registros de eventos de carga snapshot de dealers
    /// - Información de paginación
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="eventoCargaProcesoId">Filtrar por ID de evento de carga de proceso</param>
    /// <param name="dealerBac">Filtrar por código BAC del dealer (búsqueda parcial)</param>
    /// <param name="dms">Filtrar por sistema DMS (búsqueda parcial)</param>
    /// <param name="sincronizado">Filtrar por estado de sincronización (0 = no sincronizado, 1 = sincronizado). Si no se envía, retorna todos.</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Lista de registros de eventos de carga snapshot de dealers con información de paginación</returns>
    /// <response code="200">Operación exitosa. Retorna lista de eventos de carga snapshot de dealers con paginación.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EventoCargaSnapshotDealerDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerTodos(
        [FromQuery] int? eventoCargaProcesoId = null,
        [FromQuery] string? dealerBac = null,
        [FromQuery] string? dms = null,
        [FromQuery] int? sincronizado = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "[{CorrelationId}] 🔷 Inicio GET /evento-carga-snapshot-dealer. Filtros: EventoCargaProcesoId={EventoCargaProcesoId}, DealerBac={DealerBac}, DMS={Dms}, Sincronizado={Sincronizado}, Página={Page}, PageSize={PageSize}",
                correlationId, eventoCargaProcesoId?.ToString() ?? "null", dealerBac ?? "null", dms ?? "null", sincronizado?.ToString() ?? "null", page, pageSize);

            var (resultados, totalRecords) = await _service.ObtenerTodosConFiltrosAsync(
                eventoCargaProcesoId,
                dealerBac,
                dms,
                sincronizado,
                page,
                pageSize);

            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] ✅ GET /evento-carga-snapshot-dealer completado en {ElapsedMs}ms. {Count} registros obtenidos de {Total} totales (Página {Page} de {TotalPages})",
                correlationId, stopwatch.ElapsedMilliseconds, resultados.Count, totalRecords, page, totalPages);

            return Ok(new ApiResponse<List<EventoCargaSnapshotDealerDto>>
            {
                Success = true,
                Message = resultados.Count > 0
                    ? $"Se obtuvieron {resultados.Count} registros (Página {page} de {totalPages})"
                    : "No se encontraron registros que coincidan con los filtros",
                Data = resultados,
                Pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = totalPages
                },
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "[{CorrelationId}] ❌ Error en GET /evento-carga-snapshot-dealer después de {ElapsedMs}ms",
                correlationId, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error interno del servidor",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}

