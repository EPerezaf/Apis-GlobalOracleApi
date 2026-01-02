using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.EventoCargaProceso;

/// <summary>
/// Controller para consulta de eventos de carga de proceso.
/// Ruta base: /api/v1/gm/catalog-sync/evento-carga-proceso
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/evento-carga-proceso")]
[Produces("application/json")]
[Authorize]
public class GetEventoCargaProcesoController : ControllerBase
{
    private readonly IEventoCargaProcesoService _service;
    private readonly ILogger<GetEventoCargaProcesoController> _logger;

    public GetEventoCargaProcesoController(
        IEventoCargaProcesoService service,
        ILogger<GetEventoCargaProcesoController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los registros de eventos de carga de proceso con filtros opcionales
    /// </summary>
    /// <remarks>
    /// Este endpoint permite obtener el listado de eventos de carga de proceso con filtros opcionales.
    /// 
    /// **Parámetros opcionales:**
    /// - `proceso`: Filtrar por nombre del proceso (búsqueda parcial, ej: "ProductsCatalog")
    /// - `idCarga`: Filtrar por ID de carga (búsqueda parcial, ej: "products_catalog_16122025")
    /// - `actual`: Filtrar por estado actual (true = carga actual, false = carga histórica)
    /// 
    /// **Ejemplos de uso:**
    /// - GET /api/v1/gm/catalog-sync/evento-carga-proceso
    /// - GET /api/v1/gm/catalog-sync/evento-carga-proceso?proceso=ProductsCatalog
    /// - GET /api/v1/gm/catalog-sync/evento-carga-proceso?actual=true
    /// - GET /api/v1/gm/catalog-sync/evento-carga-proceso?proceso=ProductsCatalog&amp;actual=true
    /// 
    /// **Campos en la respuesta:**
    /// - `eventoCargaProcesoId`: ID único del registro
    /// - `proceso`: Nombre del proceso de sincronización
    /// - `nombreArchivo`: Nombre del archivo cargado
    /// - `fechaCarga`: Fecha y hora de la carga
    /// - `idCarga`: Identificador único de la carga (debe ser único)
    /// - `registros`: Cantidad de registros procesados
    /// - `actual`: Indica si es la carga actual (true) o histórica (false)
    /// - `tablaRelacion`: Nombre de la tabla relacionada (opcional)
    /// - `componenteRelacionado`: Componente relacionado (opcional)
    /// - Campos de auditoría: fechaAlta, usuarioAlta, fechaModificacion, usuarioModificacion
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Lista de registros de eventos de carga
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="proceso">Filtrar por nombre del proceso (búsqueda parcial)</param>
    /// <param name="idCarga">Filtrar por ID de carga (búsqueda parcial)</param>
    /// <param name="actual">Filtrar por estado actual (true=actual, false=histórico)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Lista de registros de eventos de carga de proceso con información de paginación</returns>
    /// <response code="200">Operación exitosa. Retorna lista de eventos de carga con paginación.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EventoCargaProcesoDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerTodos(
        [FromQuery] string? proceso = null,
        [FromQuery] string? idCarga = null,
        [FromQuery] bool? actual = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var userId = JwtUserHelper.GetCurrentUser(User, _logger);

        _logger.LogInformation(
            "[{CorrelationId}] 📋 Inicio GET /evento-carga-proceso. Usuario: {UserId}, Filtros: Proceso={Proceso}, IdCarga={IdCarga}, Actual={Actual}, Página={Page}, PageSize={PageSize}",
            correlationId, userId, proceso ?? "null", idCarga ?? "null", actual?.ToString() ?? "null", page, pageSize);

        try
        {
            var (resultados, totalRecords) = await _service.ObtenerTodosConFiltrosAsync(proceso, idCarga, actual, page, pageSize);

            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] ✅ GET /evento-carga-proceso completado en {ElapsedMs}ms. {Cantidad} registros obtenidos de {Total} totales (Página {Page} de {TotalPages})",
                correlationId, stopwatch.ElapsedMilliseconds, resultados.Count, totalRecords, page, totalPages);

            return Ok(new ApiResponse<List<EventoCargaProcesoDto>>
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
                "[{CorrelationId}] ❌ Error en GET /evento-carga-proceso. Tiempo: {ElapsedMs}ms",
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
    /// Obtiene un registro de evento de carga de proceso por su ID
    /// </summary>
    /// <remarks>
    /// Este endpoint permite obtener un registro específico de evento de carga de proceso por su identificador único.
    /// 
    /// **Ejemplo de uso:**
    /// - GET /api/v1/gm/catalog-sync/evento-carga-proceso/1
    /// - GET /api/v1/gm/catalog-sync/evento-carga-proceso/123
    /// 
    /// **Campos en la respuesta:**
    /// - `eventoCargaProcesoId`: ID único del registro
    /// - `proceso`: Nombre del proceso de sincronización
    /// - `nombreArchivo`: Nombre del archivo cargado
    /// - `fechaCarga`: Fecha y hora de la carga
    /// - `idCarga`: Identificador único de la carga
    /// - `registros`: Cantidad de registros procesados
    /// - `actual`: Indica si es la carga actual (true) o histórica (false)
    /// - `tablaRelacion`: Nombre de la tabla relacionada (opcional)
    /// - `componenteRelacionado`: Componente relacionado (opcional)
    /// - Campos de auditoría: fechaAlta, usuarioAlta, fechaModificacion, usuarioModificacion
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Registro de evento de carga solicitado
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="id">ID del registro (COCP_EVENTOCARGAPROCESOID)</param>
    /// <returns>Registro de evento de carga de proceso</returns>
    /// <response code="200">Operación exitosa. Retorna el registro de evento de carga.</response>
    /// <response code="404">No se encontró el registro con el ID especificado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<EventoCargaProcesoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var userId = JwtUserHelper.GetCurrentUser(User, _logger);

        _logger.LogInformation(
            "[{CorrelationId}] 🔍 Inicio GET /evento-carga-proceso/{Id}. Usuario: {UserId}",
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
                    Message = $"No se encontró el registro de evento de carga con ID {id}",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] ✅ GET /evento-carga-proceso/{Id} completado en {ElapsedMs}ms",
                correlationId, id, stopwatch.ElapsedMilliseconds);

            return Ok(new ApiResponse<EventoCargaProcesoDto>
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
                "[{CorrelationId}] ❌ Error en GET /evento-carga-proceso/{Id}. Tiempo: {ElapsedMs}ms",
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

