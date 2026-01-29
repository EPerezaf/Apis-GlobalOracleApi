using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.Empleados;

[ApiController]
[Route("api/v1/common/erp/empleados/catalogo-documentos")]
[Produces("application/json")]
[Tags("ERP")]
public class GetCatalogoDocExpedienteController : ControllerBase
{
    private readonly ICatalogoDocExpedienteService _service;
    private readonly ILogger<GetCatalogoDocExpedienteController> _logger;
    public GetCatalogoDocExpedienteController(
        ICatalogoDocExpedienteService service,
        ILogger<GetCatalogoDocExpedienteController> logger)
    {
        _service = service;
        _logger = logger;    
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CatalogoDocResponseDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> ObtenerCatalogoDocExpediente(
        [FromQuery] int? idEmpleado = null,
        [FromQuery] int? claveTipoDocumento = null,
        [FromQuery] int? idDocumento = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200)
    {
        var userInfo = JwtUserHelper.GetCurrentUserInfo(User, _logger);
        var correlationId = CorrelationHelper.GenerateCorrelationId();
        var currentUser = JwtUserHelper.GetCurrentUser(User, _logger);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Inicio de obtencion de catalogo de documentos. Usuario: {UserId}, CorrelationId: {CorrelationId}, EmpresaId: {EmpresaId}, Parametros: {@Params}",
                currentUser, correlationId, userInfo.EmpresaId, 
                new 
                { 
                    idDocumento, 
                    idEmpleado, 
                    claveTipoDocumento, 
                    page, 
                    pageSize
                });
            
            var(data, totalRecords) = await _service.ObtenerCatalogoAsync(
                claveTipoDocumento, idEmpleado, userInfo.EmpresaId, idDocumento, null, page, pageSize, currentUser, correlationId);

            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            stopwatch.Stop();
            _logger.LogInformation(
                "Catalogo de documentos obtenidos exitosamente. CorrelationId: {Correlationid}, Tiempo: {ElapsedMs}ms, Registros: {Count} de Total: {Total}",
                correlationId, stopwatch.ElapsedMilliseconds, data.Count, totalRecords);
            
            return Ok(new ApiResponse<List<CatalogoDocResponseDto>>
            {
                Success = true,
                Message = data.Count > 0
                    ? $"Registros obtenidos exitosamente (Pagina {page} de {totalPages})"
                    : "No se encontraron registros que coincidan con los filtros",
                Data = data,
                Pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = totalPages
                }
            });
        }
        catch(EmpleadoValidacionException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
            "Error de valdiacion al obtener Catalogo de Documentos. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
            correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (EmpleadoDataAccessException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
            "Error de acceso a datos al obtener Catalogo de Documentos. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
            correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = "Error de acceso a la base de datos. Por favor, intente nuevamente.",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
            "Error inesperado al obtener Catalogo de Documentos. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario. {UserId}",
            correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return BadRequest(new ApiResponse
            {
                Success  = false,
                Message = "Error inesperado al procesar la solicitud. Por favor, intente nuevamente",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        
    }
    
    
}