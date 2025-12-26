using GM.DealersSincronizacion.Application.DTOs;
using GM.DealersSincronizacion.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;
using System.Diagnostics;

namespace GM.DealersSincronizacion.API.Controllers;

/// <summary>
/// Controller para obtener productos activos.
/// </summary>
[ApiController]
[Route("api/v1/gm/dealer-sinc-productos/productos")]
[Authorize]
public class GetProductosController : ControllerBase
{
    private readonly IProductoService _productoService;
    private readonly ILogger<GetProductosController> _logger;

    public GetProductosController(
        IProductoService productoService,
        ILogger<GetProductosController> logger)
    {
        _productoService = productoService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los productos activos con paginación.
    /// </summary>
    /// <remarks>
    /// Este endpoint retorna una lista paginada de productos activos desde la tabla `CO_GM_LISTAPRODUCTOS`.
    /// El dealerBac se obtiene automáticamente del token JWT para futuras validaciones o filtros por dealer.
    /// 
    /// **Funcionalidad:**
    /// - Consulta todos los productos activos de la tabla `CO_GM_LISTAPRODUCTOS`
    /// - Aplica paginación para optimizar el rendimiento y reducir el tamaño de la respuesta
    /// - Retorna información de paginación (página actual, total de páginas, total de registros)
    /// 
    /// **Parámetros opcionales:**
    /// - `page`: Número de página a consultar (por defecto: 1, mínimo: 1)
    /// - `pageSize`: Cantidad de registros por página (por defecto: 200, máximo recomendado: 200)
    /// 
    /// **Ejemplos de uso:**
    /// - GET /api/v1/gm/dealer-sinc-productos/productos
    /// - GET /api/v1/gm/dealer-sinc-productos/productos?page=1&amp;pageSize=200
    /// - GET /api/v1/gm/dealer-sinc-productos/productos?page=2&amp;pageSize=100
    /// 
    /// **Campos en la respuesta:**
    /// - `productoId`: ID único del producto
    /// - `codigo`: Código del producto
    /// - `nombre`: Nombre del producto
    /// - `descripcion`: Descripción del producto
    /// - `precio`: Precio del producto
    /// - `stock`: Stock disponible
    /// - `activo`: Indica si el producto está activo
    /// 
    /// **Validaciones:**
    /// - El usuario debe estar autenticado (JWT requerido)
    /// - El dealerBac se obtiene automáticamente del token JWT
    /// - La página debe ser mayor a 0
    /// - El tamaño de página debe estar entre 1 y 200 (recomendado)
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Lista paginada de productos activos
    /// - Información de paginación (página actual, total de páginas, total de registros)
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="page">Número de página (por defecto: 1, mínimo: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200, máximo recomendado: 200)</param>
    /// <returns>Lista paginada de productos activos con información de paginación</returns>
    /// <response code="200">Operación exitosa. Retorna lista de productos con paginación.</response>
    /// <response code="401">No autorizado si no se proporciona un token JWT válido.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ProductoDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> ObtenerProductos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var dealerBac = JwtUserHelper.GetDealerBac(User, _logger);
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "🔷 [CONTROLLER] Obteniendo productos. DealerBac: {DealerBac}, Página: {Page}, CorrelationId: {CorrelationId}",
            dealerBac, page, correlationId);

        try
        {
            var (data, totalRecords) = await _productoService.ObtenerTodosAsync(page, pageSize);

            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            stopwatch.Stop();
            _logger.LogInformation(
                "✅ [CONTROLLER] Productos obtenidos. DealerBac: {DealerBac}, Tiempo: {ElapsedMs}ms, Registros: {Count} de {Total}, Página: {Page} de {TotalPages}",
                dealerBac, stopwatch.ElapsedMilliseconds, data.Count, totalRecords, page, totalPages);

            return Ok(new ApiResponse<List<ProductoDto>>
            {
                Success = true,
                Message = data.Count > 0
                    ? $"Registros obtenidos exitosamente (Página {page} de {totalPages})"
                    : "No se encontraron productos activos",
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
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "❌ [CONTROLLER] Error al obtener productos. DealerBac: {DealerBac}", dealerBac);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error interno del servidor",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}

