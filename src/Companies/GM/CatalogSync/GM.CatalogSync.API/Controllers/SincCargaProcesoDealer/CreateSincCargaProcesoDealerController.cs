using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Exceptions;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.SincCargaProcesoDealer;

/// <summary>
/// Controller para creación de sincronización de carga de proceso por dealer.
/// Ruta base: /api/v1/gm/catalog-sync/sinc-carga-proceso-dealer
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/sinc-carga-proceso-dealer")]
[Produces("application/json")]
[Authorize]
public class CreateSincCargaProcesoDealerController : ControllerBase
{
    private readonly ISincCargaProcesoDealerService _service;
    private readonly ILogger<CreateSincCargaProcesoDealerController> _logger;

    public CreateSincCargaProcesoDealerController(
        ISincCargaProcesoDealerService service,
        ILogger<CreateSincCargaProcesoDealerController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Crea un nuevo registro de sincronización de carga de proceso por dealer
    /// </summary>
    /// <remarks>
    /// Este endpoint permite registrar una nueva sincronización de carga de proceso para un dealer específico.
    /// 
    /// **Funcionalidad de la transacción:**
    /// - Automáticamente actualiza los contadores en `CO_EVENTOSCARGAPROCESO`:
    ///   - `COCP_DEALERSSINCRONIZADOS`: Cuenta total de dealers sincronizados para el `eventoCargaProcesoId`
    ///   - `COCP_PORCDEALERSSINC`: Porcentaje calculado (DealersSincronizados / DealersTotales * 100)
    /// - Si falla cualquier operación, se hace ROLLBACK completo (no se crea el registro ni se actualizan contadores)
    /// - Garantiza consistencia de datos (todo o nada)
    /// 
    /// **Validaciones:**
    /// - La combinación de `eventoCargaProcesoId` y `dealerBac` debe ser única
    /// - Si ya existe un registro con la misma combinación, retorna error 409 Conflict con la fecha de sincronización previa
    /// - Debe existir un registro de evento de carga activo (`COCP_ACTUAL=1`) con el `eventoCargaProcesoId` especificado
    /// - Si no existe el registro de evento de carga, retorna error 404 Not Found
    /// - El `dealerBac` se obtiene del JWT token y debe existir en `CO_DISTRIBUIDORES` (columna `DEALERID`)
    /// - Si no existe el dealer, retorna error 404 Not Found
    /// 
    /// **Campos obligatorios en el Request Body:**
    /// - `eventoCargaProcesoId`: ID del evento de carga de proceso relacionado (FK, número, ej: 12)
    /// 
    /// **Campos calculados automáticamente (NO enviar en el request):**
    /// - `proceso`: Se obtiene de `CO_EVENTOSCARGAPROCESO.COCP_PROCESO` mediante JOIN
    /// - `registrosSincronizados`: Se obtiene de `CO_EVENTOSCARGAPROCESO.COCP_REGISTROS`
    /// - `dmsOrigen`: Se consulta de `CO_DISTRIBUIDORES.CODI_DMS` usando `dealerBac` del JWT (DEALERID). Si está vacío, se asigna "GDMS"
    /// - `dealerBac`: Se toma del JWT token (ej: "290487")
    /// - `nombreDealer`: Se consulta de `CO_DISTRIBUIDORES.CODI_NOMBRE` usando `dealerBac` del JWT (ej: "CHEVROLET CAR ONE RUIZ CORTINES")
    /// - `fechaSincronizacion`: Se calcula automáticamente con hora de México
    /// - `tokenConfirmacion`: Hash SHA256 generado automáticamente de: idCarga + dealerBac + proceso + fechaSincronizacion + registrosSincronizados
    /// - `sincCargaProcesoDealerId`: ID único generado por secuencia
    /// - `fechaAlta`: Fecha y hora del servidor (SYSDATE)
    /// - `usuarioAlta`: Se toma del JWT token
    /// - `fechaModificacion`: null (no aplica en creación)
    /// - `usuarioModificacion`: null (no aplica en creación)
    /// 
    /// **Formato del Request:**
    /// ```json
    /// {
    ///   "eventoCargaProcesoId": 12
    /// }
    /// ```
    /// 
    /// ⚠️ **IMPORTANTE:**
    /// - ❌ NO enviar `proceso` (se obtiene automáticamente de CO_EVENTOSCARGAPROCESO.COCP_PROCESO)
    /// - ❌ NO enviar `registrosSincronizados` (se obtiene automáticamente de CO_EVENTOSCARGAPROCESO.COCP_REGISTROS)
    /// - ❌ NO enviar `dmsOrigen` (se consulta de CO_DISTRIBUIDORES automáticamente)
    /// - ❌ NO enviar `dealerBac` (se toma del JWT token)
    /// - ❌ NO enviar `nombreDealer` (se consulta de CO_DISTRIBUIDORES automáticamente)
    /// - ❌ NO enviar `sincCargaProcesoDealerId` (se genera automáticamente)
    /// - ❌ NO enviar `fechaSincronizacion` (se calcula automáticamente con hora de México)
    /// - ❌ NO enviar `tokenConfirmacion` (se genera automáticamente con SHA256)
    /// - ❌ NO enviar `fechaAlta`, `usuarioAlta` (se calculan automáticamente)
    /// - ✅ La combinación `eventoCargaProcesoId` + `dealerBac` debe ser única
    /// - ✅ El `eventoCargaProcesoId` debe existir en `CO_EVENTOSCARGAPROCESO` y estar activo (`COCP_ACTUAL=1`)
    /// - ✅ El `dealerBac` del JWT debe existir en `CO_DISTRIBUIDORES` (columna `DEALERID`)
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Registro de sincronización creado con todos sus campos
    /// - ID generado automáticamente
    /// - Token de confirmación (hash SHA256) generado automáticamente
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="dto">Datos del nuevo registro de sincronización (solo eventoCargaProcesoId)</param>
    /// <returns>Registro de sincronización creado con todos los campos calculados automáticamente, incluyendo proceso y registrosSincronizados obtenidos de CO_EVENTOSCARGAPROCESO</returns>
    /// <response code="201">Registro creado exitosamente.</response>
    /// <response code="400">Error de validación en los datos enviados.</response>
    /// <response code="404">No se encontró un registro de evento de carga activo con el eventoCargaProcesoId especificado, o no se encontró el dealer en CO_DISTRIBUIDORES.</response>
    /// <response code="409">Ya existe un registro con la misma combinación eventoCargaProcesoId/dealerBac (duplicado).</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost]
    [ApiExplorerSettings(IgnoreApi = true)] // Oculto de Swagger - Este endpoint está deshabilitado
    [ProducesResponseType(typeof(ApiResponse<SincCargaProcesoDealerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Crear([FromBody] CrearSincCargaProcesoDealerDto dto)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var userId = JwtUserHelper.GetCurrentUser(User, _logger);
        var dealerBac = JwtUserHelper.GetDealerBac(User, _logger);

        _logger.LogInformation(
            "[{CorrelationId}] 📝 Inicio POST /sinc-carga-proceso-dealer. Usuario: {UserId}, DealerBac: {DealerBac}, Request: {@Request}",
            correlationId, userId, dealerBac, dto);

        try
        {
            // Validar modelo
            if (!ModelState.IsValid)
            {
                var errores = ModelState
                    .Where(ms => ms.Value?.Errors.Count > 0)
                    .SelectMany(ms => ms.Value!.Errors.Select(e => new ErrorDetail
                    {
                        Code = "VALIDATION_ERROR",
                        Field = ms.Key,
                        Message = e.ErrorMessage,
                        Details = new Dictionary<string, object>
                        {
                            { "attemptedValue", ms.Value.AttemptedValue?.ToString() ?? "N/A" }
                        }
                    }))
                    .ToList();

                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ Validación fallida. {Cantidad} error(es) encontrado(s)",
                    correlationId, errores.Count);

                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = $"Se encontraron {errores.Count} error(es) de validación. Revise los detalles en 'errors'.",
                    Errors = errores,
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            var resultado = await _service.CrearAsync(dto, userId, dealerBac);

            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] ✅ POST /sinc-carga-proceso-dealer completado en {ElapsedMs}ms. ID: {Id}, Proceso: {Proceso}, DealerBac: {DealerBac}. Contadores actualizados automáticamente.",
                correlationId, stopwatch.ElapsedMilliseconds, resultado.SincCargaProcesoDealerId, resultado.Proceso, resultado.DealerBac);

            return CreatedAtAction(
                nameof(GetSincCargaProcesoDealerController.ObtenerPorId),
                "GetSincCargaProcesoDealer",
                new { id = resultado.SincCargaProcesoDealerId },
                new ApiResponse<SincCargaProcesoDealerDto>
                {
                    Success = true,
                    Message = "Registro de sincronización creado exitosamente. Los contadores de dealers sincronizados se actualizaron automáticamente.",
                    Data = resultado,
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
        }
        catch (NotFoundException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "[{CorrelationId}] ⚠️ Registro de evento de carga no encontrado: {Mensaje}. Tiempo: {ElapsedMs}ms",
                correlationId, ex.Message, stopwatch.ElapsedMilliseconds);

            return NotFound(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<ErrorDetail>
                {
                    new ErrorDetail
                    {
                        Code = "EVENTO_NOT_FOUND",
                        Field = "eventoCargaProcesoId",
                        Message = ex.Message,
                        Details = new Dictionary<string, object>
                        {
                            { "resourceName", ex.ResourceName },
                            { "resourceId", ex.ResourceId ?? "N/A" },
                            { "suggestion", "Verifique que el eventoCargaProcesoId existe en CO_EVENTOSCARGAPROCESO y que el registro de evento de carga esté activo (COCP_ACTUAL=1)" }
                        }
                    }
                },
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (SincArchivoDealerDuplicadoException ex)
        {
            stopwatch.Stop();
            var fechaSincFormateada = ex.FechaSincronizacionPrevia.ToString("dd/MM/yyyy HH:mm:ss");
            _logger.LogWarning(
                "[{CorrelationId}] ⚠️ Registro duplicado - Proceso: {Proceso}, EventoCargaProcesoId: {EventoCargaProcesoId}, DealerBac: {DealerBac}, FechaSincronizacion: {FechaSinc}. Tiempo: {ElapsedMs}ms",
                correlationId, ex.Proceso, ex.EventoCargaProcesoId, ex.DealerBac, fechaSincFormateada, stopwatch.ElapsedMilliseconds);

            var errorDetails = new Dictionary<string, object>
            {
                { "proceso", ex.Proceso },
                { "eventoCargaProcesoId", ex.EventoCargaProcesoId },
                { "dealerBac", ex.DealerBac },
                { "fechaSincronizacion", fechaSincFormateada },
                { "constraint", "UQ_COSC_PROCESO_CARGA_DEALER" },
                { "suggestion", "La combinación de proceso, eventoCargaProcesoId y dealerBac debe ser única. Verifique si ya existe un registro con estos valores." }
            };

            return Conflict(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<ErrorDetail>
                {
                    new ErrorDetail
                    {
                        Code = "DUPLICATE_RECORD",
                        Field = "(proceso, eventoCargaProcesoId, dealerBac)",
                        Message = ex.Message,
                        Details = errorDetails
                    }
                },
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (SincArchivoDealerValidacionException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "[{CorrelationId}] ⚠️ Error de validación: {Mensaje}. Tiempo: {ElapsedMs}ms",
                correlationId, ex.Message, stopwatch.ElapsedMilliseconds);

            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<ErrorDetail>
                {
                    new ErrorDetail
                    {
                        Code = "VALIDATION_ERROR",
                        Message = ex.Message,
                        Details = new Dictionary<string, object>
                        {
                            { "suggestion", "Revise los campos requeridos y sus formatos según la documentación del endpoint" }
                        }
                    }
                },
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "[{CorrelationId}] ❌ Error en POST /sinc-carga-proceso-dealer. Tiempo: {ElapsedMs}ms",
                correlationId, stopwatch.ElapsedMilliseconds);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error interno del servidor. El error ha sido registrado.",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}

