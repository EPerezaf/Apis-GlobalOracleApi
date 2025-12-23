using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.SincArchivosDealers;

/// <summary>
/// Controller para consulta de sincronización de archivos por dealer.
/// Ruta base: /api/v1/gm/catalog-sync/sinc-archivos-dealers
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/sinc-archivos-dealers")]
[Produces("application/json")]
[Authorize]
public class GetSincArchivosDealersController : ControllerBase
{
    private readonly ISincArchivoDealerService _service;
    private readonly ILogger<GetSincArchivosDealersController> _logger;

    public GetSincArchivosDealersController(
        ISincArchivoDealerService service,
        ILogger<GetSincArchivosDealersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los registros de sincronización de archivos por dealer con filtros opcionales
    /// </summary>
    /// <remarks>
    /// Este endpoint permite obtener el listado de sincronizaciones de archivos por dealer con filtros opcionales.
    /// 
    /// **Parámetros opcionales:**
    /// - `proceso`: Filtrar por nombre del proceso (búsqueda parcial, ej: "ProductsCatalog")
    /// - `cargaArchivoSincronizacionId`: Filtrar por ID de carga de archivo de sincronización (número, ej: 1)
    /// - `dealerBac`: Filtrar por código BAC del dealer (búsqueda parcial, ej: "MX001")
    /// 
    /// **Ejemplos de uso:**
    /// - GET /api/v1/gm/catalog-sync/sinc-archivos-dealers
    /// - GET /api/v1/gm/catalog-sync/sinc-archivos-dealers?proceso=ProductsCatalog
    /// - GET /api/v1/gm/catalog-sync/sinc-archivos-dealers?dealerBac=MX001
    /// - GET /api/v1/gm/catalog-sync/sinc-archivos-dealers?proceso=ProductsCatalog&amp;cargaArchivoSincronizacionId=1
    /// 
    /// **Campos en la respuesta:**
    /// - `sincArchivoDealerId`: ID único del registro
    /// - `proceso`: Nombre del proceso de sincronización
    /// - `cargaArchivoSincronizacionId`: ID de la carga de archivo de sincronización relacionada (FK)
    /// - `idCarga`: ID de la carga (desde CO_CARGAARCHIVOSINCRONIZACION, ej: "products_catalog_16122025_1335")
    /// - `procesoCarga`: Proceso de la carga (desde CO_CARGAARCHIVOSINCRONIZACION, ej: "ProductsCatalog")
    /// - `fechaCarga`: Fecha de carga del archivo (desde CO_CARGAARCHIVOSINCRONIZACION)
    /// - `tiempoSincronizacionHoras`: Tiempo de sincronización en horas (calculado: FechaSincronizacion - FechaCarga, ej: 0.97)
    /// - `dmsOrigen`: Sistema DMS origen
    /// - `dealerBac`: Código BAC del dealer
    /// - `nombreDealer`: Nombre del dealer
    /// - `fechaSincronizacion`: Fecha de sincronización
    /// - `registrosSincronizados`: Cantidad de registros sincronizados
    /// - Campos de auditoría: fechaAlta, usuarioAlta, fechaModificacion, usuarioModificacion
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Lista de registros de sincronización
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="proceso">Filtrar por nombre del proceso (búsqueda parcial)</param>
    /// <param name="cargaArchivoSincronizacionId">Filtrar por ID de carga de archivo de sincronización (número)</param>
    /// <param name="dealerBac">Filtrar por código BAC del dealer (búsqueda parcial)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Lista de registros de sincronización de archivos por dealer con información de paginación</returns>
    /// <response code="200">Operación exitosa. Retorna lista de sincronizaciones con paginación.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SincArchivoDealerDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerTodos(
        [FromQuery] string? proceso = null,
        [FromQuery] int? cargaArchivoSincronizacionId = null,
        [FromQuery] string? dealerBac = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var userId = JwtUserHelper.GetCurrentUser(User, _logger);

        _logger.LogInformation(
            "[{CorrelationId}] 📋 Inicio GET /sinc-archivos-dealers. Usuario: {UserId}, Filtros: Proceso={Proceso}, CargaArchivoSincronizacionId={CargaArchivoSincronizacionId}, DealerBac={DealerBac}, Página={Page}, PageSize={PageSize}",
            correlationId, userId, proceso ?? "null", cargaArchivoSincronizacionId?.ToString() ?? "null", dealerBac ?? "null", page, pageSize);

        try
        {
            var (resultados, totalRecords) = await _service.ObtenerTodosConFiltrosAsync(proceso, cargaArchivoSincronizacionId, dealerBac, page, pageSize);

            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] ✅ GET /sinc-archivos-dealers completado en {ElapsedMs}ms. {Cantidad} registros obtenidos de {Total} totales (Página {Page} de {TotalPages})",
                correlationId, stopwatch.ElapsedMilliseconds, resultados.Count, totalRecords, page, totalPages);

            return Ok(new ApiResponse<List<SincArchivoDealerDto>>
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
                "[{CorrelationId}] ❌ Error en GET /sinc-archivos-dealers. Tiempo: {ElapsedMs}ms",
                correlationId, stopwatch.ElapsedMilliseconds);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error interno del servidor. El error ha sido registrado.",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }

    /// <summary>
    /// Obtiene un registro de sincronización de archivos por dealer por su ID
    /// </summary>
    /// <remarks>
    /// Este endpoint permite obtener un registro específico de sincronización por su identificador único.
    /// 
    /// **Ejemplo de uso:**
    /// - GET /api/v1/gm/catalog-sync/sinc-archivos-dealers/1
    /// - GET /api/v1/gm/catalog-sync/sinc-archivos-dealers/123
    /// 
    /// **Campos en la respuesta:**
    /// - `sincArchivoDealerId`: ID único del registro
    /// - `proceso`: Nombre del proceso de sincronización
    /// - `cargaArchivoSincronizacionId`: ID de la carga de archivo de sincronización relacionada (FK)
    /// - `idCarga`: ID de la carga (desde CO_CARGAARCHIVOSINCRONIZACION, ej: "products_catalog_16122025_1335")
    /// - `procesoCarga`: Proceso de la carga (desde CO_CARGAARCHIVOSINCRONIZACION, ej: "ProductsCatalog")
    /// - `fechaCarga`: Fecha de carga del archivo (desde CO_CARGAARCHIVOSINCRONIZACION)
    /// - `tiempoSincronizacionHoras`: Tiempo de sincronización en horas (calculado: FechaSincronizacion - FechaCarga, ej: 0.97)
    /// - `dmsOrigen`: Sistema DMS origen
    /// - `dealerBac`: Código BAC del dealer
    /// - `nombreDealer`: Nombre del dealer
    /// - `fechaSincronizacion`: Fecha de sincronización
    /// - `registrosSincronizados`: Cantidad de registros sincronizados
    /// - Campos de auditoría: fechaAlta, usuarioAlta, fechaModificacion, usuarioModificacion
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Registro de sincronización solicitado
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="id">ID del registro (COSA_SINCARCHIVODEALERID)</param>
    /// <returns>Registro de sincronización de archivos por dealer</returns>
    /// <response code="200">Operación exitosa. Retorna el registro de sincronización.</response>
    /// <response code="404">No se encontró el registro con el ID especificado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<SincArchivoDealerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var userId = JwtUserHelper.GetCurrentUser(User, _logger);

        _logger.LogInformation(
            "[{CorrelationId}] 🔍 Inicio GET /sinc-archivos-dealers/{Id}. Usuario: {UserId}",
            correlationId, id, userId);

        try
        {
            var resultado = await _service.ObtenerPorIdAsync(id);

            if (resultado == null)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ Registro con ID {Id} no encontrado. Tiempo: {ElapsedMs}ms",
                    correlationId, id, stopwatch.ElapsedMilliseconds);

                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = $"No se encontró el registro de sincronización con ID {id}",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] ✅ GET /sinc-archivos-dealers/{Id} completado en {ElapsedMs}ms",
                correlationId, id, stopwatch.ElapsedMilliseconds);

            return Ok(new ApiResponse<SincArchivoDealerDto>
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
                "[{CorrelationId}] ❌ Error en GET /sinc-archivos-dealers/{Id}. Tiempo: {ElapsedMs}ms",
                correlationId, id, stopwatch.ElapsedMilliseconds);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error interno del servidor. El error ha sido registrado.",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}

