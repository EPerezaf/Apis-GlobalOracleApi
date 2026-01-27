using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.Empleados;
[ApiController]
[Route("api/v1/common/erp/empleados/empleados-historico-asignacion")]
[Produces("application/json")]
[Authorize]
[Tags("ERP")]
public class GetEmpleadosHistoricoController: ControllerBase
{
    private readonly IEmpleadoHistoricoService _service;
    private readonly ILogger<GetEmpleadosHistoricoController> _logger;
    public GetEmpleadosHistoricoController(
        IEmpleadoHistoricoService service,
        ILogger<GetEmpleadosHistoricoController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EmpleadoHistoricoRespuestaDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> ObtenerEmpleadosHistorico(
        [FromQuery] int? idAsignacion = null,
        [FromQuery] int? idEmpleado = null,
        [FromQuery] string? dealerId = null,
        [FromQuery] string? clavePuesto = null,
        [FromQuery] string? departamento = null,
        [FromQuery] int? esActual = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200)
    {
        var userInfo = JwtUserHelper.GetCurrentUserInfo(User, _logger);
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var currentUser = JwtUserHelper.GetCurrentUser(User, _logger);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Inicio de obtencion de empleados historico. Usuario: {UserId}, CorrelationId: {CorrelationId}, EmpresaId: {EmpresaId}, Parametros: {@Params}",
                currentUser, correlationId, userInfo.EmpresaId, new { idAsignacion,idEmpleado, dealerId,clavePuesto,departamento, page,pageSize});
            
            var (data, totalRecords) = await _service.ObtenerEmpleadosHistoricosAsync(
                idAsignacion, idEmpleado, dealerId, clavePuesto, departamento,esActual, userInfo.EmpresaId, page, pageSize, currentUser, correlationId);
            
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            stopwatch.Stop();
            _logger.LogInformation(
                "Empleados Historico obtenidos exitosamente. CorrelationId: {Correlationid}, tiempo: {ElapsedMs}ms, Registros: {Count} de {Total}, Pagina: {Page} de {TotalPages}",
                correlationId, stopwatch.ElapsedMilliseconds, data.Count, totalRecords,page, totalPages);
            
            return Ok(new ApiResponse<List<EmpleadoHistoricoRespuestaDto>>
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
                },
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (EmpleadoValidacionException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "Error de validacion al obtener empleados historico. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser);
            
            return BadRequest(new ApiResponse
            {
                Success= true,
                Message = ex.Message,
                Data = ex.Data,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (EmpleadoDataAccessException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error de acceso a datos al obtener empleados historico. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser);
            
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Ocurrió un error al procesar la solicitud",
                Data = ex.Data,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error inesperado al obtener empleados historico. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser);
            
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Ocurrió un error inesperado al procesar la solicitud",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
    
}
