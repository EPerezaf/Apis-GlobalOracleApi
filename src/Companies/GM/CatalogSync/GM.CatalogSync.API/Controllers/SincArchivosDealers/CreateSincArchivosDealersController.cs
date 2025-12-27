using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Exceptions;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.SincArchivosDealers;

/// <summary>
/// Controller para creación de sincronización de archivos por dealer.
/// Ruta base: /api/v1/gm/catalog-sync/sinc-archivos-dealers
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/sinc-archivos-dealers")]
[Produces("application/json")]
[Authorize]
public class CreateSincArchivosDealersController : ControllerBase
{
    private readonly ISincArchivoDealerService _service;
    private readonly ILogger<CreateSincArchivosDealersController> _logger;

    public CreateSincArchivosDealersController(
        ISincArchivoDealerService service,
        ILogger<CreateSincArchivosDealersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Crea un nuevo registro de sincronización de archivos por dealer
    /// </summary>
    /// <remarks>
    /// Este endpoint permite registrar una nueva sincronización de archivos para un dealer específico.
    /// 
    /// **Funcionalidad de la transacción:**
    /// - Automáticamente actualiza los contadores en `CO_CARGAARCHIVOSINCRONIZACION`:
    ///   - `COCA_DEALERSSONCRONIZADOS`: Cuenta total de dealers sincronizados para el `cargaArchivoSincronizacionId`
    ///   - `COCA_PORCDEALERSSINC`: Porcentaje calculado (DealersSincronizados / DealersTotales * 100)
    /// - Si falla cualquier operación, se hace ROLLBACK completo (no se crea el registro ni se actualizan contadores)
    /// - Garantiza consistencia de datos (todo o nada)
    /// 
    /// **Validaciones:**
    /// - La combinación de `cargaArchivoSincronizacionId` y `dealerBac` debe ser única
    /// - Si ya existe un registro con la misma combinación, retorna error 409 Conflict con la fecha de sincronización previa
    /// - Debe existir un registro de carga activo (`COCA_ACTUAL=1`) con el `cargaArchivoSincronizacionId` especificado
    /// - Si no existe el registro de carga, retorna error 404 Not Found
    /// - El `dealerBac` debe existir en `CO_DISTRIBUIDORES` (columna `DEALERID`)
    /// - Si no existe el dealer, retorna error 404 Not Found
    /// 
    /// **Campos obligatorios en el Request Body:**
    /// - `cargaArchivoSincronizacionId`: ID de la carga de archivo de sincronización relacionada (FK, número, ej: 12)
    /// - `dealerBac`: Código BAC del dealer (DEALERID en CO_DISTRIBUIDORES, ej: "319334")
    /// 
    /// **Campos calculados automáticamente (NO enviar en el request):**
    /// - `proceso`: Se obtiene de `CO_CARGAARCHIVOSINCRONIZACION.COCA_PROCESO` mediante JOIN
    /// - `registrosSincronizados`: Se obtiene de `CO_CARGAARCHIVOSINCRONIZACION.COCA_REGISTROS`
    /// - `dmsOrigen`: Se consulta de `CO_DISTRIBUIDORES.CODI_DMS` usando `dealerBac` (DEALERID). Si está vacío, se asigna "GDMS"
    /// - `nombreDealer`: Se consulta de `CO_DISTRIBUIDORES.CODI_NOMBRE` usando `dealerBac` (DEALERID)
    /// - `fechaSincronizacion`: Se calcula automáticamente con hora de México
    /// - `tokenConfirmacion`: Hash SHA256 generado automáticamente de: idCarga + dealerBac + fechaSincronizacion + registrosSincronizados
    /// - `sincArchivoDealerId`: ID único generado por secuencia
    /// - `fechaAlta`: Fecha y hora del servidor (SYSDATE)
    /// - `usuarioAlta`: Se toma del JWT token
    /// - `fechaModificacion`: null (no aplica en creación)
    /// - `usuarioModificacion`: null (no aplica en creación)
    /// 
    /// **Formato del Request:**
    /// ```json
    /// {
    ///   "cargaArchivoSincronizacionId": 12,
    ///   "dealerBac": "319334"
    /// }
    /// ```
    /// 
    /// ⚠️ **IMPORTANTE:**
    /// - ❌ NO enviar `proceso` (se obtiene automáticamente de CO_CARGAARCHIVOSINCRONIZACION.COCA_PROCESO)
    /// - ❌ NO enviar `registrosSincronizados` (se obtiene automáticamente de CO_CARGAARCHIVOSINCRONIZACION.COCA_REGISTROS)
    /// - ❌ NO enviar `dmsOrigen` (se consulta de CO_DISTRIBUIDORES automáticamente)
    /// - ❌ NO enviar `nombreDealer` (se consulta de CO_DISTRIBUIDORES automáticamente)
    /// - ❌ NO enviar `sincArchivoDealerId` (se genera automáticamente)
    /// - ❌ NO enviar `fechaSincronizacion` (se calcula automáticamente con hora de México)
    /// - ❌ NO enviar `tokenConfirmacion` (se genera automáticamente con SHA256)
    /// - ❌ NO enviar `fechaAlta`, `usuarioAlta` (se calculan automáticamente)
    /// - ✅ La combinación `cargaArchivoSincronizacionId` + `dealerBac` debe ser única
    /// - ✅ El `cargaArchivoSincronizacionId` debe existir en `CO_CARGAARCHIVOSINCRONIZACION` y estar activo (`COCA_ACTUAL=1`)
    /// - ✅ El `dealerBac` debe existir en `CO_DISTRIBUIDORES` (columna `DEALERID`)
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Registro de sincronización creado con todos sus campos
    /// - ID generado automáticamente
    /// - Token de confirmación (hash SHA256) generado automáticamente
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="dto">Datos del nuevo registro de sincronización (solo cargaArchivoSincronizacionId y dealerBac)</param>
    /// <returns>Registro de sincronización creado con todos los campos calculados automáticamente, incluyendo proceso y registrosSincronizados obtenidos de CO_CARGAARCHIVOSINCRONIZACION</returns>
    /// <response code="201">Registro creado exitosamente.</response>
    /// <response code="400">Error de validación en los datos enviados.</response>
    /// <response code="404">No se encontró un registro de carga activo con el cargaArchivoSincronizacionId especificado, o no se encontró el dealer en CO_DISTRIBUIDORES.</response>
    /// <response code="409">Ya existe un registro con la misma combinación cargaArchivoSincronizacionId/dealerBac (duplicado).</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost]
    [ApiExplorerSettings(IgnoreApi = true)] // Oculto de Swagger - Este endpoint está deshabilitado
    [ProducesResponseType(typeof(ApiResponse<SincArchivoDealerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Crear([FromBody] CrearSincArchivoDealerDto dto)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var userId = JwtUserHelper.GetCurrentUser(User, _logger);

        _logger.LogInformation(
            "[{CorrelationId}] 📝 Inicio POST /sinc-archivos-dealers. Usuario: {UserId}, Request: {@Request}",
            correlationId, userId, dto);

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

            var resultado = await _service.CrearAsync(dto, userId);

            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] ✅ POST /sinc-archivos-dealers completado en {ElapsedMs}ms. ID: {Id}, Proceso: {Proceso}, DealerBac: {DealerBac}. Contadores actualizados automáticamente.",
                correlationId, stopwatch.ElapsedMilliseconds, resultado.SincArchivoDealerId, resultado.Proceso, resultado.DealerBac);

            return CreatedAtAction(
                nameof(GetSincArchivosDealersController.ObtenerPorId),
                "GetSincArchivosDealers",
                new { id = resultado.SincArchivoDealerId },
                new ApiResponse<SincArchivoDealerDto>
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
                "[{CorrelationId}] ⚠️ Registro de carga no encontrado: {Mensaje}. Tiempo: {ElapsedMs}ms",
                correlationId, ex.Message, stopwatch.ElapsedMilliseconds);

            return NotFound(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<ErrorDetail>
                {
                    new ErrorDetail
                    {
                        Code = "CARGA_NOT_FOUND",
                        Field = "cargaArchivoSincronizacionId",
                        Message = ex.Message,
                        Details = new Dictionary<string, object>
                        {
                            { "resourceName", ex.ResourceName },
                            { "resourceId", ex.ResourceId ?? "N/A" },
                            { "suggestion", "Verifique que el cargaArchivoSincronizacionId existe en CO_CARGAARCHIVOSINCRONIZACION y que el registro de carga esté activo (COCA_ACTUAL=1)" }
                        }
                    }
                },
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (SincArchivoDealerDuplicadoException ex)
        {
            stopwatch.Stop();
            var fechaSincFormateada = ex.FechaSincronizacion?.ToString("dd/MM/yyyy HH:mm:ss") ?? "N/A";
            _logger.LogWarning(
                "[{CorrelationId}] ⚠️ Registro duplicado - Proceso: {Proceso}, CargaArchivoSincronizacionId: {CargaArchivoSincronizacionId}, DealerBac: {DealerBac}, FechaSincronizacion: {FechaSinc}. Tiempo: {ElapsedMs}ms",
                correlationId, ex.Proceso, ex.CargaArchivoSincronizacionId, ex.DealerBac, fechaSincFormateada, stopwatch.ElapsedMilliseconds);

            var errorDetails = new Dictionary<string, object>
            {
                { "proceso", ex.Proceso },
                { "cargaArchivoSincronizacionId", ex.CargaArchivoSincronizacionId },
                { "dealerBac", ex.DealerBac },
                { "constraint", "UQ_COSA_PROCESO_CARGA_DEALER" },
                { "suggestion", "La combinación de proceso, cargaArchivoSincronizacionId y dealerBac debe ser única. Verifique si ya existe un registro con estos valores." }
            };

            if (ex.FechaSincronizacion.HasValue)
            {
                errorDetails.Add("fechaSincronizacion", fechaSincFormateada);
            }

            return Conflict(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<ErrorDetail>
                {
                    new ErrorDetail
                    {
                        Code = "DUPLICATE_RECORD",
                        Field = "(proceso, cargaArchivoSincronizacionId, dealerBac)",
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
                "[{CorrelationId}] ❌ Error en POST /sinc-archivos-dealers. Tiempo: {ElapsedMs}ms",
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

