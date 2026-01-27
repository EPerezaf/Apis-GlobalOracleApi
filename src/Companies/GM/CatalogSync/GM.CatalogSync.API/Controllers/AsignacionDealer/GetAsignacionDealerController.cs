using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.AsignacionDealers;

[ApiController]
[Route("api/seguridad/empresas/usuarios/{userId}/distribuidores/asignados")]
[Authorize]
[Tags("AsignacionDealer")]
public class GetAsignacionDealerController : ControllerBase
{
    private readonly IAsignacionService _service;
    private readonly ILogger<GetAsignacionDealerController> _logger;
    public GetAsignacionDealerController(
        IAsignacionService service,
        ILogger<GetAsignacionDealerController> logger)
    {
        _service = service;
        _logger = logger;    
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<AsignacionRespuestaDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> ObtenerAsignaciones(
        [FromRoute] string userId,
        //[FromQuery] string? dealer = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var currentUser = JwtUserHelper.GetCurrentUser(User, _logger);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Inicio de obtencion de asignaciones. Usuario: {UserId}, CorrelationId: {CorrelationId}, Parametros: {@Params}",
                currentUser, correlationId, new { userId, page, pageSize});

            var (data, totalRecords) = await _service.ObtenerAsignacionesAsync(
                userId, page, pageSize, currentUser, correlationId);
            
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            stopwatch.Stop();
            _logger.LogInformation(
                "Asginaciones obtenidas exitosamente. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Registros: {Count} de {Total}, Pagina: {Page} de {TotalPages}",
                correlationId, stopwatch.ElapsedMilliseconds, data.Count, totalRecords, page, totalPages);

            return Ok(new ApiResponse<List<AsignacionRespuestaDto>>
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
        catch (AsignacionValidacionException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
            "Error de validacion al obtener asignaciones. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
            correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Data = ex.Data,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch(AsignacionDataAccessException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
            "Error de acceso a datos al obtner asignaciones. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
            correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error al acceder a la base de datos. Por favor , intente nuevamente",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error inesperado al obtener asignaciones. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {userId}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser);
            
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error inesperado del servidor. El error ha sido registrado.",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }

    [HttpGet]
    [Route("/api/seguridad/empresas/usuarios/disponibles-distribuidores")]
    [ProducesResponseType(typeof(ApiResponse<List<AsignacionRespuestaDto>>),200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    [Tags("AsignacionDealer")]
    
    public async Task<IActionResult> ObtenerUsuariosDisponibles(
        [FromQuery] string? userId = null,
        [FromQuery] string? nombre = null,
        [FromQuery] string? email = null,
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
                "Inicio de obtencion de usurios disponibles para asignacion. Usuairo: {UserId}, CorrelationId: {CorrelationId}, EmpresaId: {EmpresaId}, Parametros: {@Params}",
                currentUser,
                correlationId,
                userInfo.EmpresaId,
                new
                {
                    userId,
                    nombre,
                    email,
                    page,
                    pageSize
                }
            );

            var (data, totalRecords) = await _service.ObtenerUsuariosDisponiblesAsync(
                userId,
                nombre, 
                email,
                userInfo.EmpresaId,
                page,
                pageSize,
                currentUser,
                correlationId
            );

            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            stopwatch.Stop();
            _logger.LogInformation(
                "Usuarios disponibles obtenidos exitosamente. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Registros: {Count} de {Total}, Pagina: {Page} de {TotalPages}",
                correlationId, stopwatch.ElapsedMilliseconds, data.Count, totalRecords, page, totalPages);
            
            return Ok(new ApiResponse
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
                Timestamp = DateTimeHelper.GetMexicoTimeString(),
            });

        }
        catch (AsignacionValidacionException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
            "Error de validacion al obtener usuarios disponibles para asignacion. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
            correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error al acceder a la base de datos. Por favor , intente nuevamente",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch(AsignacionDataAccessException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
            "Error de acceso a datos al obtner asignaciones. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
            correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error al acceder a la base de datos. Por favor , intente nuevamente",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error inesperado al obtener asignaciones. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {userId}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser);
            
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error inesperado del servidor. El error ha sido registrado.",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }

    [HttpGet]
    [Route("/api/seguridad/empresas/usuarios/{userId}/distribuidores/disponibles-asignables")]
    [ProducesResponseType(typeof(ApiResponse<List<DetalleDealerRespuestaDto>>),200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    [Tags("AsignacionDealer")]
    public async Task<IActionResult>ObtenerDistribuidoresAsignables(
        [FromRoute] string userId,
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
                "Inicio de obtencion de distribuidores asignables. Usuario: {UserId}, CorrelationId: {CorrelationId}, EmpresaId: {EmpresaId}, Parametros: {@Params}",
                currentUser,
                correlationId,
                userInfo.EmpresaId,
                new
                {
                    userId,
                    page,
                    pageSize
                }
            );

            var (data, totalRecords) = await _service.ObtenerDistribuidoresAsignablesAsync(
                userId,
                userInfo.EmpresaId,
                page,
                pageSize,
                currentUser,
                correlationId
            );

            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            stopwatch.Stop();
            _logger.LogInformation(
                "Distribuidores asignables obtenidos exitosamente. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Registros: {Count} de {Total}, Pagina: {Page} de {TotalPages}",
                correlationId, stopwatch.ElapsedMilliseconds, data.Count, totalRecords, page, totalPages);
             
            return Ok(new ApiResponse
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
                Timestamp = DateTimeHelper.GetMexicoTimeString(),
            });
        }   
        catch (AsignacionValidacionException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
            "Error de validacion al obtener distribuidores asignables. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
            correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error al acceder a la base de datos. Por favor , intente nuevamente",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch(AsignacionDataAccessException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
            "Error de acceso a datos al obtner distribuidores asignables. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
            correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error al acceder a la base de datos. Por favor , intente nuevamente",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error inesperado al obtener distribuidores asignables. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {userId}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser);
            
            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error inesperado del servidor. El error ha sido registrado.",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
    
}