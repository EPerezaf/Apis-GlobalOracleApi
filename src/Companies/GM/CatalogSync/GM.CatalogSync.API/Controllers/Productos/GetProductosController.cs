using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Exceptions;
using Shared.Security;
using System.Diagnostics;

namespace GM.CatalogSync.API.Controllers.Productos;

/// <summary>
/// Controller para operaciones GET de productos
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/product-list")]
[Produces("application/json")]
[Authorize]
[Tags("ProductList")]
public class GetProductosController : ControllerBase
{
    private readonly IProductoService _service;
    private readonly ILogger<GetProductosController> _logger;

    public GetProductosController(
        IProductoService service,
        ILogger<GetProductosController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el listado de productos con filtros opcionales
    /// </summary>
    /// <remarks>
    /// Este endpoint permite obtener el listado de productos con la posibilidad de aplicar filtros opcionales.
    /// 
    /// **Parámetros opcionales:**
    /// - `pais`: Filtrar por país (ej: "Mexico")
    /// - `marcaNegocio`: Filtrar por marca (ej: "Chevrolet")
    /// - `anioModelo`: Filtrar por año (ej: 2025)
    /// - `page`: Número de página (por defecto: 1)
    /// - `pageSize`: Tamaño de página (por defecto: 200)
    /// 
    /// **Ejemplos de uso:**
    /// - GET /api/v1/gm/catalog-sync/product-list
    /// - GET /api/v1/gm/catalog-sync/product-list?pais=Mexico
    /// - GET /api/v1/gm/catalog-sync/product-list?pais=Mexico&amp;marcaNegocio=Chevrolet&amp;anioModelo=2025
    /// - GET /api/v1/gm/catalog-sync/product-list?page=1&amp;pageSize=50
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Lista de productos que coinciden con los filtros
    /// - Información de paginación (página actual, total de páginas, total de registros)
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="pais">Filtrar por país (ej: "Mexico")</param>
    /// <param name="marcaNegocio">Filtrar por marca (ej: "Chevrolet")</param>
    /// <param name="anioModelo">Filtrar por año (ej: 2025)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Lista de productos con información de paginación</returns>
    /// <response code="200">Operación exitosa. Retorna lista de productos.</response>
    /// <response code="400">Error de validación en los parámetros.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ProductoRespuestaDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> ObtenerProductos(
        [FromQuery] string? pais = null,
        [FromQuery] string? marcaNegocio = null,
        [FromQuery] int? anioModelo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var currentUser = JwtUserHelper.GetCurrentUser(User, _logger);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Inicio de obtención de productos. Usuario: {UserId}, CorrelationId: {CorrelationId}, Parámetros: {@Params}",
                currentUser, correlationId, new { pais, marcaNegocio, anioModelo, page, pageSize });

            var (data, totalRecords) = await _service.ObtenerProductosAsync(
                pais, marcaNegocio, anioModelo, page, pageSize, currentUser, correlationId);

            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            stopwatch.Stop();
            _logger.LogInformation(
                "Productos obtenidos exitosamente. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Registros: {Count} de {Total}, Página: {Page} de {TotalPages}",
                correlationId, stopwatch.ElapsedMilliseconds, data.Count, totalRecords, page, totalPages);

            return Ok(new ApiResponse<List<ProductoRespuestaDto>>
            {
                Success = true,
                Message = data.Count > 0
                    ? $"Registros obtenidos exitosamente (Página {page} de {totalPages})"
                    : "No se encontraron registros que coincidan con los filtros",
                Data = data,
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
        catch (ProductoValidacionException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "Error de validación al obtener productos. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Data = ex.Errors,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (ProductoDataAccessException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error de acceso a datos al obtener productos. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error al acceder a la base de datos. Por favor, intente nuevamente.",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error inesperado al obtener productos. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
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

