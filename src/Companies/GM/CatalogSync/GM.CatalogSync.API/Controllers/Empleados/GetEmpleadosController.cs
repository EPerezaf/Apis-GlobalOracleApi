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
[Route("api/v1/gm/empleados/obtener-empleados")]
[Produces("application/json")]
[Authorize]
[Tags("Empleados")]
public class GetEmpleadosController : ControllerBase
{
    private readonly IEmpleadoService _service;
    private readonly ILogger<GetEmpleadosController> _logger;
    public GetEmpleadosController(
        IEmpleadoService service,
        ILogger<GetEmpleadosController> logger)
    {
            _service = service;
            _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<EmpleadoRespuestaDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> ObtenerEmpleados(
        [FromQuery] int? idEmpleado = null,
        [FromQuery] int? dealerId = null,
        [FromQuery] string? curp = null,
        [FromQuery] string? numeroEmpleado = null,
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
                "Inicio de obtención de empleados. Usuario: {UserId}, CorrelationId: {CorrelationId}, EmpresaId: {EmpresaId}, Parámetros: {@Params}",
                currentUser, correlationId, userInfo.EmpresaId, new { idEmpleado,dealerId,curp,numeroEmpleado, page, pageSize });

            var (data, totalRecords) = await _service.ObtenerEmpleadosAsync(
                idEmpleado,dealerId,curp,numeroEmpleado, userInfo.EmpresaId, page, pageSize, currentUser, correlationId);

            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            stopwatch.Stop();
            _logger.LogInformation(
                "Empleados obtenidos exitosamente. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Registros: {Count} de {Total}, Página: {Page} de {TotalPages}",
                correlationId, stopwatch.ElapsedMilliseconds, data.Count, totalRecords, page, totalPages);


            return Ok(new ApiResponse<List<EmpleadoRespuestaDto>>
            {
                Success =true,
                Message = data.Count > 0
                    ? $"Registros obtenidos exitosamente (Paina {page} de {totalPages})"
                    : "No se encontraron registros que coincidan con los filtros",
                Data = data,
                Pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = totalPages
                },
                Timestamp =DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (EmpleadoValidacionException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "Error de validación al obtener empleados. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Data = ex.Errors,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (EmpleadoDataAccessException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error de acceso a datos al obtener empleados. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
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
                "Error inesperado al obtener empleados. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
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