using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Exceptions;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.EventoCargaSnapshotDealer;

/// <summary>
/// Controller para creación batch de eventos de carga snapshot de dealers.
/// Ruta base: /api/v1/gm/catalog-sync/evento-carga-snapshot-dealer
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/evento-carga-snapshot-dealer")]
[Produces("application/json")]
[Authorize]
public class CreateEventoCargaSnapshotDealerController : ControllerBase
{
    private readonly IEventoCargaSnapshotDealerService _service;
    private readonly ILogger<CreateEventoCargaSnapshotDealerController> _logger;

    public CreateEventoCargaSnapshotDealerController(
        IEventoCargaSnapshotDealerService service,
        ILogger<CreateEventoCargaSnapshotDealerController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Crea múltiples registros de eventos de carga snapshot de dealers en batch
    /// </summary>
    /// <remarks>
    /// Este endpoint permite crear múltiples registros de eventos de carga snapshot de dealers en una sola operación (batch insert).
    /// **Los distribuidores se generan automáticamente desde CO_DISTRIBUIDORES** basándose en el empresaId del JWT token.
    /// 
    /// **Este endpoint NO recibe payload en el body. El `eventoCargaProcesoId` se pasa como parámetro de ruta.**
    /// 
    /// **Funcionalidad automática:**
    /// - Los distribuidores se obtienen automáticamente desde `CO_DISTRIBUIDORES`
    /// - Se filtran por `EMPR_EMPRESAID` (obtenido del JWT token)
    /// - Se genera un registro automáticamente para cada distribuidor encontrado
    /// - Los registros que ya existen se omiten automáticamente (no se insertan duplicados)
    /// 
    /// **Funcionalidad de la transacción:**
    /// - Todos los registros se insertan en una sola transacción
    /// - Si falla cualquier registro, se hace ROLLBACK completo de todos los registros
    /// - Garantiza integridad de datos (todo o nada)
    /// 
    /// **Validaciones:**
    /// - El `eventoCargaProcesoId` debe existir en `CO_EVENTOSCARGAPROCESO`
    /// - Se valida la llave única (COSD_COCP_EVENTOCARGAPROCESOID, COSD_DEALERBAC) ANTES de insertar
    /// - Los registros duplicados se omiten automáticamente (no se insertan)
    /// - Si todos los distribuidores ya tienen registro, retorna lista vacía sin insertar
    /// 
    /// **Parámetros obligatorios:**
    /// - `eventoCargaProcesoId`: ID del evento de carga de proceso (FK) - se pasa como parámetro de ruta
    /// 
    /// **Campos generados automáticamente desde CO_DISTRIBUIDORES:**
    /// - `dealerBac`: Se obtiene de `CO_DISTRIBUIDORES.DEALERID`
    /// - `nombreDealer`: Se obtiene de `CO_DISTRIBUIDORES.CODI_NOMBRE`
    /// - `razonSocialDealer`: Se obtiene de `CO_DISTRIBUIDORES.CODI_RAZONSOCIAL`
    /// - `dms`: Se obtiene de `CO_DISTRIBUIDORES.CODI_DMS` (si está vacío, se usa "GDMS" por defecto)
    /// - `urlWebhook`: Se obtiene de `CO_DISTRIBUIDORES.CODI_URLWEBHOOK`
    /// - `secretKey`: Se obtiene de `CO_DISTRIBUIDORES.CODI_SECRETKEY`
    /// 
    /// **Campos calculados automáticamente:**
    /// - `fechaRegistro`: Se calcula automáticamente con hora de México
    /// - `eventoCargaSnapshotDealerId`: ID único generado por secuencia de Oracle
    /// - `fechaAlta`: Fecha y hora del servidor Oracle (SYSDATE)
    /// - `usuarioAlta`: Se toma del JWT token
    /// 
    /// **Ejemplo de uso:**
    /// ```
    /// POST /api/v1/gm/catalog-sync/evento-carga-snapshot-dealer/15
    /// ```
    /// 
    /// ⚠️ **IMPORTANTE:**
    /// - ❌ NO se envía payload en el body (el endpoint no recibe JSON)
    /// - ❌ NO enviar `dealerBac`, `nombreDealer`, `razonSocialDealer`, `dms`, `urlWebhook`, `secretKey` (se obtienen automáticamente de CO_DISTRIBUIDORES)
    /// - ❌ NO enviar `eventoCargaSnapshotDealerId` (se genera automáticamente)
    /// - ❌ NO enviar `fechaRegistro` (se calcula automáticamente con hora de México)
    /// - ❌ NO enviar campos de auditoría (se calculan automáticamente)
    /// - ❌ NO enviar `usuarioAlta` (se toma del JWT token)
    /// - ✅ Solo se requiere `eventoCargaProcesoId` como parámetro de ruta
    /// - ✅ Los distribuidores se filtran automáticamente por empresaId del JWT
    /// - ✅ La combinación (eventoCargaProcesoId, dealerBac) debe ser única
    /// - ✅ Todos los registros se procesan en una sola transacción
    /// - ✅ Los registros duplicados se omiten automáticamente (no se insertan)
    /// 
    /// **Proceso interno:**
    /// 1. Validar que existe `eventoCargaProcesoId` en `CO_EVENTOSCARGAPROCESO`
    /// 2. Obtener empresaId del JWT token
    /// 3. Consultar todos los distribuidores desde `CO_DISTRIBUIDORES` filtrados por empresaId
    /// 4. Generar registros automáticamente para cada distribuidor encontrado
    /// 5. Validar duplicados en BD (query SELECT COUNT) - ANTES de insertar
    /// 6. Filtrar solo los registros nuevos (omitir duplicados)
    /// 7. Si no hay registros nuevos, retornar lista vacía
    /// 8. Si hay registros nuevos, iniciar transacción
    /// 9. INSERT: Crear todos los registros nuevos en batch
    /// 10. COMMIT o ROLLBACK según resultado
    /// 
    /// **Ejemplo de respuesta exitosa:**
    /// Si hay 50 distribuidores y 10 ya tienen registro:
    /// - Se insertarán 40 registros nuevos
    /// - Se omitirán 10 registros duplicados
    /// - La respuesta incluirá los 40 registros creados
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Lista de registros creados con todos sus campos
    /// - IDs generados automáticamente
    /// - Timestamp de la operación
    /// - Información de cuántos distribuidores se procesaron y cuántos se crearon
    /// </remarks>
    /// <param name="eventoCargaProcesoId">ID del evento de carga de proceso (FK) - se pasa como parámetro de ruta</param>
    /// <returns>Lista de registros creados</returns>
    /// <response code="201">Registros creados exitosamente.</response>
    /// <response code="400">Error de validación en los datos enviados.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost("{eventoCargaProcesoId}")]
    [ProducesResponseType(typeof(ApiResponse<List<EventoCargaSnapshotDealerDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CrearBatch([FromRoute] int eventoCargaProcesoId)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var userId = JwtUserHelper.GetCurrentUser(User, _logger);

        _logger.LogInformation(
            "[{CorrelationId}] 📝 Inicio POST /evento-carga-snapshot-dealer/{EventoCargaProcesoId} (batch). Usuario: {UserId}",
            correlationId, eventoCargaProcesoId, userId);

        try
        {
            if (eventoCargaProcesoId <= 0)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ Validación fallida: EventoCargaProcesoId debe ser mayor a 0. Valor recibido: {EventoCargaProcesoId}",
                    correlationId, eventoCargaProcesoId);

                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "El ID de evento de carga de proceso es requerido y debe ser mayor a 0.",
                    Errors = new List<ErrorDetail>
                    {
                        new ErrorDetail
                        {
                            Code = "VALIDATION_ERROR",
                            Field = "eventoCargaProcesoId",
                            Message = "El ID de evento de carga debe ser mayor a 0",
                            Details = new Dictionary<string, object> { { "attemptedValue", eventoCargaProcesoId } }
                        }
                    },
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            var empresaId = JwtUserHelper.GetEmpresaId(User, _logger);
            if (!empresaId.HasValue)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ EmpresaId no encontrado en el token JWT",
                    correlationId);

                return Unauthorized(new ApiResponse
                {
                    Success = false,
                    Message = "EmpresaId no encontrado en el token JWT. El usuario debe estar autenticado con un token válido que contenga el claim EMPRESAID.",
                    Errors = new List<ErrorDetail>
                    {
                        new ErrorDetail
                        {
                            Code = "UNAUTHORIZED",
                            Field = "empresaId",
                            Message = "EmpresaId no encontrado en el token JWT",
                            Details = new Dictionary<string, object>
                            {
                                { "suggestion", "Verifique que el token JWT contenga el claim EMPRESAID o EMPR_EMPRESAID" }
                            }
                        }
                    },
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            var usuario = JwtUserHelper.GetCurrentUser(User, _logger);

            var resultados = await _service.CrearBatchAsync(eventoCargaProcesoId, usuario, empresaId.Value);

            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] ✅ POST /evento-carga-snapshot-dealer/{EventoCargaProcesoId} (batch) completado en {ElapsedMs}ms. {Cantidad} registros creados",
                correlationId, eventoCargaProcesoId, stopwatch.ElapsedMilliseconds, resultados.Count);

            return CreatedAtAction(
                nameof(GetEventoCargaSnapshotDealerController.ObtenerTodos),
                "GetEventoCargaSnapshotDealer",
                null,
                new ApiResponse<List<EventoCargaSnapshotDealerDto>>
                {
                    Success = true,
                    Message = $"Se crearon exitosamente {resultados.Count} registros de eventos de carga snapshot de dealers",
                    Data = resultados,
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
        }
        catch (BusinessValidationException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "[{CorrelationId}] ⚠️ Error de validación: {Mensaje}. {Cantidad} error(es) detallado(s). Tiempo: {ElapsedMs}ms",
                correlationId, ex.Message, ex.Errors?.Count ?? 0, stopwatch.ElapsedMilliseconds);

            var errorDetails = ex.Errors?.Select(err => new ErrorDetail
            {
                Code = err.Field.Contains("eventoCargaProcesoId") ? "CARGA_NOT_FOUND" : "DUPLICATE_RECORD",
                Field = err.Field,
                Message = err.Message,
                Details = new Dictionary<string, object>
                {
                    { "attemptedValue", err.AttemptedValue ?? "N/A" },
                    { "constraint", err.Field.Contains("eventoCargaProcesoId") ? "FK_COSD_COCP_EVENTOCARGAPROCESOID" : "UQ_COSD_CARGA_DEALER" },
                    { "suggestion", err.Field.Contains("eventoCargaProcesoId")
                        ? "Verifique que el eventoCargaProcesoId existe en CO_EVENTOSCARGAPROCESO"
                        : "La combinación de eventoCargaProcesoId y dealerBac debe ser única. Verifique si ya existe un registro con estos valores." }
                }
            }).ToList() ?? new List<ErrorDetail>();

            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Errors = errorDetails,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "[{CorrelationId}] ❌ Error en POST /evento-carga-snapshot-dealer/{EventoCargaProcesoId} (batch). Tiempo: {ElapsedMs}ms",
                correlationId, eventoCargaProcesoId, stopwatch.ElapsedMilliseconds);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error interno del servidor. El error ha sido registrado.",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}

