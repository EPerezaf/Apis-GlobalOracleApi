using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;
using System.Diagnostics;

namespace GM.CatalogSync.API.Controllers.Campanias;

/// <summary>
/// Controller para operaciones GET de campanias
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/campaign-list")]
[Produces("application/json")]
[Authorize]
[Tags("CampaignList")]
public class GetCampaignsController : ControllerBase
{
    private readonly ICampaignService _service;
    private readonly ILogger<GetCampaignsController> _logger;

    public GetCampaignsController(
        ICampaignService service,
        ILogger<GetCampaignsController> logger)
    {
        _service = service;
        _logger = logger;
    }

/// <summary>
    /// Obtiene el listado de campanias con filtros opcionales
    /// </summary>
    /// <remarks>
    /// Este endpoint permite obtener el listado de campanias con la posibilidad de aplicar filtros opcionales.
    /// 
    /// **Parámetros opcionales:**
    /// - `id`: Filtrar por id (ej: "701Vy00000MoaZvIAJ")
    /// - `leadRecordType`: Filtrar por lead record type (ej: "New Vehicle Sales")
    /// - `page`: Número de página (por defecto: 1)
    /// - `pageSize`: Tamaño de página (por defecto: 200)
    /// 
    /// **Ejemplos de uso:**
    /// - GET /api/v1/gm/catalog-sync/campaign-list
    /// - GET /api/v1/gm/catalog-sync/campaign-list?id=701Vy00000MoaZvIAJ
    /// - GET /api/v1/gm/catalog-sync/campaign-list?leadRecordType=New Vehicle Sales
    /// - GET /api/v1/gm/catalog-sync/campaign-list?page=1&amp;pageSize=50
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Lista de campanias que coinciden con los filtros
    /// - Información de paginación (página actual, total de páginas, total de registros)
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="id">Filtrar por id (ej: "701Vy00000MoaZvIAJ")</param>
    /// <param name="leadRecordType">Filtrar por lead record type (ej: "New Vehicle Sales")</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Lista de campanias con información de paginación</returns>
    /// <response code="200">Operación exitosa. Retorna lista de campanias.</response>
    /// <response code="400">Error de validación en los parámetros.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CampaignResponseDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> GetCampaigns(
        [FromQuery] string? id = null,
        [FromQuery] string? leadRecordType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200)

    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var currentUser = JwtUserHelper.GetCurrentUser(User, _logger);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Inicio de obtención de campanias. Usuario: {UserId}, CorrelationId: {CorrelationId}, Parámetros: {@Params}",
                currentUser, correlationId, new { id, leadRecordType, page, pageSize });

            var (data, totalRecords) = await _service.GetCampaignsAsync(
                id, leadRecordType, page, pageSize, currentUser, correlationId);

            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            stopwatch.Stop();
            _logger.LogInformation(
                "Campanias obtenidas exitosamente. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Registros: {Count} de {Total}, Página: {Page} de {TotalPages}",
                correlationId, stopwatch.ElapsedMilliseconds, data.Count, totalRecords, page, totalPages);

            return Ok(new ApiResponse<List<CampaignResponseDto>>
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
        catch (CampaignValidationException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "Error de validación al obtener campanias. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Data = ex.Errors,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (CampaignDataAccessException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error de acceso a datos al obtener campanias. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
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
                "Error inesperado al obtener campanias. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
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

