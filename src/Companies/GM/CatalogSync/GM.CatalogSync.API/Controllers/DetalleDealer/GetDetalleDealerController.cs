using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.DetallerDealer;

[ApiController]
[Route("api/v1/jt/detalle-dealer")]
[Produces("application/json")]
[Tags("DetalleDealer")]

public class GetDetalleDealerController : ControllerBase
{
    private readonly IDetalleService _service;
    private readonly ILogger<GetDetalleDealerController> _logger;
    public GetDetalleDealerController(
        IDetalleService service,
        ILogger<GetDetalleDealerController> logger)
    {
        _service = service;
        _logger = logger;    
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<DetalleDealerRespuestaDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> ObtenerDealers(
        [FromQuery] string? dealerId = null,
        [FromQuery] string? nombre = null,
        [FromQuery] string? razonSocial = null,
        [FromQuery] string? rfc = null,
        [FromQuery] int? noDealer = null,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 200)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var currentUser = JwtUserHelper.GetCurrentUser(User, _logger);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Inicio de obtenecion de dealers. Usuario: {UserId}, CorrelationId: {CorrelationId}, Parametros: {@Params}",
                currentUser, correlationId, new { dealerId, nombre, razonSocial, rfc, noDealer });

                var (data, totalRecords) = await _service.ObtenerDelearAsync(
                    dealerId, nombre, razonSocial, rfc, noDealer, page, pageSize, currentUser, correlationId);
                
                
                int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                stopwatch.Stop();
                _logger.LogInformation(
                    "Dealers obtenidos exitosamente. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Registros {Count} de {Total}, Pagina: {Page} de {PageSize}",
                    correlationId, stopwatch.ElapsedMilliseconds, data.Count, totalRecords, page, totalPages);
                
                return Ok(new ApiResponse<List<DetalleDealerRespuestaDto>>
                {
                    Success = true,
                    Message = data.Count > 0
                        ? $"Registros obtenidos exitosamente (Pagina {page} de {totalRecords})"
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
        catch (DetalleDealerValidacionException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "Error de validación al obtener detalle dealer. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Data = ex.Errors,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (DetalleDealerDataAccessException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error de acceso a datos al obtener detalle dealer. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
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
                "Error de acceso a datos al obtener detalle dealer. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error al acceder a la base de datos. Por favor, intente nuevamente.",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
    
    
}