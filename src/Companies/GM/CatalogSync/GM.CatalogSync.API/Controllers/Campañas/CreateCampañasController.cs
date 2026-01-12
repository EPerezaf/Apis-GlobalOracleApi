using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.Campañas;


[ApiController]
[Route("api/v1/gm/catalog-sync/campaign-list-batch-insert")]
[Produces("application/json")]
[Tags("CampaignList")]
public class CreateCampañasController : ControllerBase
{
    private readonly ICampañaService _service;
    private readonly ILogger<CreateCampañasController> _logger;

    public CreateCampañasController( 
        ICampañaService service,
        ILogger<CreateCampañasController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CampañaBatchResultadoDto>), 200 )]
    [ProducesResponseType(typeof(ApiResponse),400)]
    [ProducesResponseType(typeof(ApiResponse),500)]

    public async Task<IActionResult>CrearCampañas([FromBody] CampañaBatchRequestDto request)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var currentUser = JwtUserHelper.GetCurrentUser(User, _logger);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            //VALIDAR QUE EL ARRAY DE CAMPAÑAS NO SEA NULO O VACIO
            if(request.Json == null || request.Json.Count == 0)
            {
                return BadRequest(new ApiResponse<CampañaBatchResultadoDto>
                {
                    Success = false,
                    Message = "El array 'json' es requerido y debe contener al menos una camapaña",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            _logger.LogInformation( 
                    "Inicio de creacion de campañas (batch). Usuario: {UserId}, CorrelationId: {CorrelationId}, TotalRegistros: {TotalRegistros}, Request: {@Request}",
                    currentUser, correlationId, request.Json.Count, request);
            var result = await _service.ProcesarBatchInsertAsync( 
                request.Json,
                currentUser, 
                correlationId);

            stopwatch.Stop();
            _logger.LogInformation( 
                "Campañas creados exitosamente (batch). CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}, Insertados: {Insertados}, Actualizados: {Actualizados}, Errores: {Errores}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser, result.RegistrosInsertados, result.RegistrosActualizados, result.RegistrosError);

                return Ok(new ApiResponse<CampañaBatchResultadoDto>
                {
                    Success = true,
                    Message = result.RegistrosInsertados > 0
                        ? $"Operacion Batch completada exitosamente: {result.RegistrosInsertados} camapana(s) insertado(s)"
                        : "No se insertaron campañas",
                    Data = result,
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
        }
        catch (CampañaDuplicadoException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "Duplicados detectados al crear campañas. CorrelatioId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}, Duplicados: {DuplicateCount}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser, ex.CantidadDuplicados);

                return BadRequest(new ApiResponse
                {
                    Success = false, 
                    Message = ex.Message,
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
        }
        catch (CampañaValidacionException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
             "Error de validacion al crear campañas. CorrelatioId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}, Duplicados: {DuplicateCount}",
             correlationId, stopwatch.ElapsedMilliseconds, currentUser, ex.Errors?.Count ?? 0);

             
            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Data = ex.Errors,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (CampañaDataAccessException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error de acceso a datos al crear campañas. CorrelatioId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error al acceder a la base de datos. Por favor intente nuevamente.",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error inesperado al crear campañas. CorrelatioId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
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