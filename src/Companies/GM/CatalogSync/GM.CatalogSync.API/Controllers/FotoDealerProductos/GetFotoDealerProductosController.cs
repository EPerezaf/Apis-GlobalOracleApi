using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.FotoDealerProductos;

/// <summary>
/// Controller para consulta de fotos de dealer productos.
/// Ruta base: /api/v1/gm/catalog-sync/foto-dealer-productos
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/foto-dealer-productos")]
[Produces("application/json")]
[Authorize]
public class GetFotoDealerProductosController : ControllerBase
{
    private readonly IFotoDealerProductosService _service;
    private readonly ILogger<GetFotoDealerProductosController> _logger;

    public GetFotoDealerProductosController(
        IFotoDealerProductosService service,
        ILogger<GetFotoDealerProductosController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los registros de fotos de dealer productos con filtros opcionales
    /// </summary>
    /// <remarks>
    /// Este endpoint permite obtener el listado de fotos de dealer productos con filtros opcionales.
    /// 
    /// **Parámetros opcionales:**
    /// - `cargaArchivoSincronizacionId`: Filtrar por ID de carga de archivo de sincronización
    /// - `dealerBac`: Filtrar por código BAC del dealer (búsqueda parcial)
    /// - `dms`: Filtrar por sistema DMS (búsqueda parcial)
    /// 
    /// **Ejemplos de uso:**
    /// - GET /api/v1/gm/catalog-sync/foto-dealer-productos
    /// - GET /api/v1/gm/catalog-sync/foto-dealer-productos?cargaArchivoSincronizacionId=1
    /// - GET /api/v1/gm/catalog-sync/foto-dealer-productos?dealerBac=DEALER001
    /// - GET /api/v1/gm/catalog-sync/foto-dealer-productos?dms=CDK
    /// 
    /// **Campos en la respuesta:**
    /// - `fotoDealerProductosId`: ID único del registro
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
    /// - Lista de registros de fotos de dealer productos
    /// - Información de paginación
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="cargaArchivoSincronizacionId">Filtrar por ID de carga de archivo de sincronización</param>
    /// <param name="dealerBac">Filtrar por código BAC del dealer (búsqueda parcial)</param>
    /// <param name="dms">Filtrar por sistema DMS (búsqueda parcial)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Lista de registros de fotos de dealer productos con información de paginación</returns>
    /// <response code="200">Operación exitosa. Retorna lista de fotos de dealer productos con paginación.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<FotoDealerProductosDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerTodos(
        [FromQuery] int? cargaArchivoSincronizacionId = null,
        [FromQuery] string? dealerBac = null,
        [FromQuery] string? dms = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "[{CorrelationId}] 🔷 Inicio GET /foto-dealer-productos. Filtros: CargaArchivoSincId={CargaId}, DealerBac={DealerBac}, DMS={Dms}, Página={Page}, PageSize={PageSize}",
                correlationId, cargaArchivoSincronizacionId?.ToString() ?? "null", dealerBac ?? "null", dms ?? "null", page, pageSize);

            var (resultados, totalRecords) = await _service.ObtenerTodosConFiltrosAsync(
                cargaArchivoSincronizacionId,
                dealerBac,
                dms,
                page,
                pageSize);

            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] ✅ GET /foto-dealer-productos completado en {ElapsedMs}ms. {Count} registros obtenidos de {Total} totales (Página {Page} de {TotalPages})",
                correlationId, stopwatch.ElapsedMilliseconds, resultados.Count, totalRecords, page, totalPages);

            return Ok(new ApiResponse<List<FotoDealerProductosDto>>
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
                "[{CorrelationId}] ❌ Error en GET /foto-dealer-productos después de {ElapsedMs}ms",
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

