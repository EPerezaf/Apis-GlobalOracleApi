using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.AsignacionDealers;

[ApiController]
[Route("api/seguridad/empresas/empresaId/usuarios/userId/distribuidores")]
[Produces("application/json")]
[Tags("AsignacionDealer")]

public class CreateAsignacionDealearsController : ControllerBase
{
    private readonly IAsignacionService _service;
    private readonly ILogger<CreateAsignacionDealearsController> _logger;

    public CreateAsignacionDealearsController( 
        IAsignacionService service,
        ILogger<CreateAsignacionDealearsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AsignacionBatchResultadoDto>),200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]

    public async Task<IActionResult> CrearAsignacion([FromBody] AsignacionBatchRequestDto request)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var currentUser = JwtUserHelper.GetCurrentUser(User, _logger);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if(request.Json == null || request.Json.Count == 0)
            {
                return BadRequest(new ApiResponse<AsignacionBatchResultadoDto>
                {
                    Success = false,
                    Message = "El array 'json' es requerido y debe contener al menos una asignacion",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            _logger.LogInformation(
                "Inicio de creacion de asignacion (batch). Usuario: {UserId}, Correlation: {CorrelationId}, TotalRegistros: {TotalRegistros}, Request: {@Request}",
                currentUser, correlationId, request.Json.Count, request);

            var result = await _service.ProcesarBatchInsertAsync(
                request.Json,
                currentUser,
                correlationId);
            stopwatch.Stop();

            _logger.LogInformation(
                "Asignacion creados exitosamente (batch). CorrelationId: {CorrelationId}, Tiempo: {ElapseedMs}ms, Usuario: {UserId}, Insertados: {INsertados}, Actualizados: {Actualizados}, Errores: {Errores}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser, result.RegistrosInsertados, result.RegistrosActualizados, result.RegistrosError);

            return Ok(new ApiResponse<AsignacionBatchResultadoDto>
            {
                Success = true,
                Message = result.RegistrosInsertados > 0
                    ? $"Operacion batch completada exitosamente: {result.RegistrosInsertados} asginacion(es) insertado(s)"
                    : "No se insertaron asignaciones",
                Data = result,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (AsignacionDuplicadoException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "Duplicados detectados al crear asignaciones. CorrelationId: {CorrlationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}, Duplicados: {DuplicateCount}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser, ex.CantidadDuplicados);

                return BadRequest( new ApiResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
        }
        catch (AsignacionDataAccessException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error de acceso a datos al crear asignaciones. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
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
                "Error inesperado al crear asignaciones. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
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