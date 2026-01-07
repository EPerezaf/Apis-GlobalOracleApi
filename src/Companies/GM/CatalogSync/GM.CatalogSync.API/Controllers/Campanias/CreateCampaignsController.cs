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
/// Controller para operaciones POST de campanias
/// </summary>

[ApiController]
[Route("api/v1/gm/catalog-sync/campaign-list-batch-insert")]
[Produces("application/json")]
[Authorize]
[Tags("CampaignList")]
public class CreateCampaignsController : ControllerBase
{
    private readonly ICampaignService _service;
    private readonly ILogger<CreateCampaignsController> _logger;

    public CreateCampaignsController(
        ICampaignService service,
        ILogger<CreateCampaignsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Crea campanias en el listado (operación batch INSERT)
    /// </summary>
    /// <remarks>
    /// Este endpoint permite crear múltiples campanias en una sola operación batch (INSERT).
    /// 
    /// **Funcionalidad INSERT:**
    /// - **Solo INSERT:** Crea nuevas campanias (no actualiza existentes)
    /// - **campaniaId se genera automáticamente:** No debe enviarse en el request
    /// - **Campos de auditoría se calculan automáticamente:** No enviar fechaAlta, usuarioAlta, fechaModificacion, usuarioModificacion
    /// - **Validación de duplicados:**
    ///   - Detecta duplicados en el mismo lote (mismo id)
    ///   - Valida contra la base de datos antes de insertar 
    ///   - Si ya existe un registro con el mismo id, retorna error claro sin intentar insertar
    /// - **TODO O NADA:** Si hay errores, rollback automático completo
    /// 
    /// **Campos obligatorios:**
    /// - `sourceCodeId`: Source Code ID
    /// - `id`: Id
    /// - `name`: Name
    /// - `recordTypeId`: Record Type ID
    /// - `leadRecordType`: Lead Record Type
    /// 
    /// **Campos opcionales:**
    /// - `leadEnquiryType`: Lead Enquiry Type
    /// - `leadSource`: Lead Source
    /// - `leadSourceDetails`: Lead Source Details
    /// - `status`: Status
    /// 
    /// **Formato del Request (camelCase sin prefijos):**
    /// ```json
    /// {
    ///   "json": [
    ///     {
    ///       "sourceCodeId": "Experiencia_Envision2023",
    ///       "id": "701Vy00000MoaZvIAJ",
    ///       "name": "AMR Experiencia Envision 2023",
    ///       "recordTypeId": "012Hs000000C94GIAS",
    ///       "leadRecordType": "New Vehicle Sales",
    ///       "leadEnquiryType": "Request a Quote",
    ///       "leadSource": "Web - 3rd Party",
    ///       "leadSourceDetails": "Aeroméxico",
    ///       "status": "Completed"
    ///     }
    ///   ]
    /// }
    /// ```
    /// 
    /// ⚠️ **IMPORTANTE:**
    /// - ❌ NO enviar campaniaId (se genera automáticamente)
    /// - ❌ NO enviar fechaAlta, usuarioAlta, fechaModificacion, usuarioModificacion (se calculan automáticamente)
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Total de registros procesados
    /// - Registros insertados exitosamente
    /// - Registros con errores (si los hay)
    /// - Información de paginación
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="request">Objeto con el array de campaigns a crear</param>
    /// <returns>Resultado de la operación batch con estadísticas</returns>
    /// <response code="200">Operación batch completada exitosamente.</response>
    /// <response code="400">Error de validación o duplicados detectados.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CampaignBatchResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> CreateCampaigns([FromBody] CampaignBatchRequestDto request)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var currentUser = JwtUserHelper.GetCurrentUser(User, _logger);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validar que el array de campanias no sea nulo o vacío
            if (request.Json == null || request.Json.Count == 0)
            {
                return BadRequest(new ApiResponse<CampaignBatchResultDto>
                {
                    Success = false,
                    Message = "El array 'json' es requerido y debe contener al menos una campaña",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            _logger.LogInformation(
                "Inicio de creación de campanias (batch). Usuario: {UserId}, CorrelationId: {CorrelationId}, TotalRegistros: {TotalRegistros}, Request: {@Request}",
                currentUser, correlationId, request.Json.Count, request);

            var result = await _service.ProcessBatchInsertAsync(
                request.Json,
                currentUser,
                correlationId);

            stopwatch.Stop();
            _logger.LogInformation(
                "Campanias creadas exitosamente (batch). CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}, Insertados: {Insertados}, Actualizados: {Actualizados}, Errores: {Errores}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser, result.RegistrosInsertados, result.RegistrosActualizados, result.RegistrosError);

            return Ok(new ApiResponse<CampaignBatchResultDto>
            {
                Success = true,
                Message = result.RegistrosInsertados > 0
                    ? $"Operación batch completada exitosamente: {result.RegistrosInsertados} campania(s) insertada(s)"
                    : "No se insertaron campanias",
                Data = result,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (CampaignDuplicateException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "Duplicados detectados al crear campanias. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}, Duplicados: {DuplicateCount}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser, ex.DuplicateCount);

            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (CampaignValidationException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "Error de validación al crear campanias. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}, Errores: {ErrorCount}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser, ex.Errors?.Count ?? 0);

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
                "Error de acceso a datos al crear campanias. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
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
                "Error inesperado al crear campanias. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
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

