using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.SincCargaProcesoDealer;

/// <summary>
/// Controller para consulta de sincronización de carga de proceso por dealer.
/// Ruta base: /api/v1/gm/catalog-sync/sinc-carga-proceso-dealer
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/sinc-carga-proceso-dealer")]
[Produces("application/json")]
[Authorize]
public class GetSincCargaProcesoDealerController : ControllerBase
{
    private readonly ISincCargaProcesoDealerService _service;
    private readonly ILogger<GetSincCargaProcesoDealerController> _logger;

    public GetSincCargaProcesoDealerController(
        ISincCargaProcesoDealerService service,
        ILogger<GetSincCargaProcesoDealerController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los registros de sincronización de carga de proceso por dealer con filtros opcionales
    /// </summary>
    /// <remarks>
    /// Este endpoint permite obtener el listado de sincronizaciones de carga de proceso por dealer con filtros opcionales.
    /// 
    /// **Parámetros opcionales:**
    /// - `proceso`: Filtrar por nombre del proceso (búsqueda parcial, ej: "ProductsCatalog")
    /// - `eventoCargaProcesoId`: Filtrar por ID de evento de carga de proceso
    /// - `dealerBac`: Filtrar por código BAC del dealer (búsqueda parcial)
    /// 
    /// **Ejemplos de uso:**
    /// - GET /api/v1/gm/catalog-sync/sinc-carga-proceso-dealer
    /// - GET /api/v1/gm/catalog-sync/sinc-carga-proceso-dealer?proceso=ProductsCatalog
    /// - GET /api/v1/gm/catalog-sync/sinc-carga-proceso-dealer?eventoCargaProcesoId=1
    /// - GET /api/v1/gm/catalog-sync/sinc-carga-proceso-dealer?dealerBac=DEALER001
    /// 
    /// **Campos en la respuesta:**
    /// - `sincCargaProcesoDealerId`: ID único del registro
    /// - `proceso`: Nombre del proceso de sincronización
    /// - `eventoCargaProcesoId`: ID del evento de carga de proceso relacionado
    /// - `idCarga`: ID de la carga (desde CO_EVENTOSCARGAPROCESO)
    /// - `procesoCarga`: Proceso de la carga (desde CO_EVENTOSCARGAPROCESO)
    /// - `fechaCarga`: Fecha de carga (desde CO_EVENTOSCARGAPROCESO)
    /// - `tiempoSincronizacionHoras`: Tiempo de sincronización en horas (diferencia entre FechaSincronizacion y FechaCarga)
    /// - `dmsOrigen`: Sistema DMS origen
    /// - `dealerBac`: Código BAC del dealer
    /// - `nombreDealer`: Nombre del dealer
    /// - `fechaSincronizacion`: Fecha de sincronización
    /// - `registrosSincronizados`: Número de registros sincronizados
    /// - `tokenConfirmacion`: Token de confirmación generado automáticamente
    /// - Campos de auditoría: fechaAlta, usuarioAlta, fechaModificacion, usuarioModificacion
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Lista de registros de sincronización
    /// - Información de paginación
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="proceso">Filtrar por nombre del proceso (búsqueda parcial)</param>
    /// <param name="eventoCargaProcesoId">Filtrar por ID de evento de carga de proceso</param>
    /// <param name="dealerBac">Filtrar por código BAC del dealer (búsqueda parcial)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Lista de registros de sincronización de carga de proceso por dealer con información de paginación</returns>
    /// <response code="200">Operación exitosa. Retorna lista de sincronizaciones con paginación.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SincCargaProcesoDealerDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerTodos(
        [FromQuery] string? proceso = null,
        [FromQuery] int? eventoCargaProcesoId = null,
        [FromQuery] string? dealerBac = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var userId = JwtUserHelper.GetCurrentUser(User, _logger);

        _logger.LogInformation(
            "[{CorrelationId}] 📋 Inicio GET /sinc-carga-proceso-dealer. Usuario: {UserId}, Filtros: Proceso={Proceso}, EventoCargaProcesoId={EventoCargaProcesoId}, DealerBac={DealerBac}, Página={Page}, PageSize={PageSize}",
            correlationId, userId, proceso ?? "null", eventoCargaProcesoId?.ToString() ?? "null", dealerBac ?? "null", page, pageSize);

        try
        {
            var (resultados, totalRecords) = await _service.ObtenerTodosConFiltrosAsync(proceso, eventoCargaProcesoId, dealerBac, page, pageSize);

            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] ✅ GET /sinc-carga-proceso-dealer completado en {ElapsedMs}ms. {Cantidad} registros obtenidos de {Total} totales (Página {Page} de {TotalPages})",
                correlationId, stopwatch.ElapsedMilliseconds, resultados.Count, totalRecords, page, totalPages);

            return Ok(new ApiResponse<List<SincCargaProcesoDealerDto>>
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
                "[{CorrelationId}] ❌ Error en GET /sinc-carga-proceso-dealer. Tiempo: {ElapsedMs}ms",
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
    /// Obtiene un registro de sincronización de carga de proceso por dealer por su ID
    /// </summary>
    /// <remarks>
    /// Este endpoint permite obtener un registro específico de sincronización de carga de proceso por dealer por su identificador único.
    /// 
    /// **Ejemplo de uso:**
    /// - GET /api/v1/gm/catalog-sync/sinc-carga-proceso-dealer/1
    /// - GET /api/v1/gm/catalog-sync/sinc-carga-proceso-dealer/123
    /// 
    /// **Campos en la respuesta:**
    /// - `sincCargaProcesoDealerId`: ID único del registro
    /// - `proceso`: Nombre del proceso de sincronización
    /// - `eventoCargaProcesoId`: ID del evento de carga de proceso relacionado
    /// - `idCarga`: ID de la carga (desde CO_EVENTOSCARGAPROCESO)
    /// - `procesoCarga`: Proceso de la carga (desde CO_EVENTOSCARGAPROCESO)
    /// - `fechaCarga`: Fecha de carga (desde CO_EVENTOSCARGAPROCESO)
    /// - `tiempoSincronizacionHoras`: Tiempo de sincronización en horas
    /// - `dmsOrigen`: Sistema DMS origen
    /// - `dealerBac`: Código BAC del dealer
    /// - `nombreDealer`: Nombre del dealer
    /// - `fechaSincronizacion`: Fecha de sincronización
    /// - `registrosSincronizados`: Número de registros sincronizados
    /// - `tokenConfirmacion`: Token de confirmación generado automáticamente
    /// - Campos de auditoría: fechaAlta, usuarioAlta, fechaModificacion, usuarioModificacion
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Registro de sincronización solicitado
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="id">ID del registro (COSC_SINCARGAPROCESODEALERID)</param>
    /// <returns>Registro de sincronización de carga de proceso por dealer</returns>
    /// <response code="200">Operación exitosa. Retorna el registro de sincronización.</response>
    /// <response code="404">No se encontró el registro con el ID especificado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<SincCargaProcesoDealerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var userId = JwtUserHelper.GetCurrentUser(User, _logger);

        _logger.LogInformation(
            "[{CorrelationId}] 🔍 Inicio GET /sinc-carga-proceso-dealer/{Id}. Usuario: {UserId}",
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
                "[{CorrelationId}] ✅ GET /sinc-carga-proceso-dealer/{Id} completado en {ElapsedMs}ms",
                correlationId, id, stopwatch.ElapsedMilliseconds);

            return Ok(new ApiResponse<SincCargaProcesoDealerDto>
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
                "[{CorrelationId}] ❌ Error en GET /sinc-carga-proceso-dealer/{Id}. Tiempo: {ElapsedMs}ms",
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

