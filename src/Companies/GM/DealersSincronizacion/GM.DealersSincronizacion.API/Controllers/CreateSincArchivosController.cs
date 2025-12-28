using GM.DealersSincronizacion.Application.DTOs;
using GM.DealersSincronizacion.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Exceptions;
using Shared.Security;
using System.Diagnostics;

namespace GM.DealersSincronizacion.API.Controllers;

/// <summary>
/// Controller para crear registros de sincronización de archivos por dealer.
/// Ruta base: /api/v1/gm/dealer-sinc/confirmar-sinc
/// </summary>
[ApiController]
[Route("api/v1/gm/dealer-sinc/confirmar-sinc")]
[Produces("application/json")]
[Authorize]
public class CreateSincArchivosController : ControllerBase
{
    private readonly ISincArchivoDealerService _sincArchivoDealerService;
    private readonly ILogger<CreateSincArchivosController> _logger;

    public CreateSincArchivosController(
        ISincArchivoDealerService sincArchivoDealerService,
        ILogger<CreateSincArchivosController> logger)
    {
        _sincArchivoDealerService = sincArchivoDealerService;
        _logger = logger;
    }

    /// <summary>
    /// Crea un nuevo registro de sincronización de archivo por dealer
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
    /// - Si ya existe un registro con la misma combinación, retorna error 400 Bad Request con la fecha de sincronización previa
    /// - Debe existir un registro de carga activo (`COCA_ACTUAL=1`) con el `cargaArchivoSincronizacionId` especificado
    /// - Si no existe el registro de carga, retorna error 400 Bad Request
    /// - El `dealerBac` debe existir en `CO_DISTRIBUIDORES`
    /// - Si no existe el dealer, retorna error 404 Not Found
    /// 
    /// **Campos obligatorios en el Request Body:**
    /// - `cargaArchivoSincronizacionId`: ID de la carga de archivo de sincronización relacionada (FK, número, ej: 12)
    /// 
    /// **Campos calculados automáticamente (NO enviar en el request):**
    /// - `proceso`: Se obtiene de `CO_CARGAARCHIVOSINCRONIZACION.COCA_PROCESO` mediante JOIN
    /// - `registrosSincronizados`: Se obtiene de `CO_CARGAARCHIVOSINCRONIZACION.COCA_REGISTROS`
    /// - `dmsOrigen`: Se consulta de `CO_DISTRIBUIDORES` usando `dealerBac` del JWT (ej: "GDMS")
    /// - `dealerBac`: Se toma del JWT token (ej: "290487")
    /// - `nombreDealer`: Se consulta de `CO_DISTRIBUIDORES` usando `dealerBac` del JWT (ej: "CHEVROLET CAR ONE RUIZ CORTINES")
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
    ///   "cargaArchivoSincronizacionId": 12
    /// }
    /// ```
    /// 
    /// ⚠️ **IMPORTANTE:**
    /// - ❌ NO enviar `proceso` (se obtiene automáticamente de CO_CARGAARCHIVOSINCRONIZACION.COCA_PROCESO)
    /// - ❌ NO enviar `registrosSincronizados` (se obtiene automáticamente de CO_CARGAARCHIVOSINCRONIZACION.COCA_REGISTROS)
    /// - ❌ NO enviar `sincArchivoDealerId` (se genera automáticamente)
    /// - ❌ NO enviar `fechaSincronizacion` (se calcula automáticamente con hora de México)
    /// - ❌ NO enviar `tokenConfirmacion` (se genera automáticamente con SHA256)
    /// - ❌ NO enviar `fechaAlta`, `usuarioAlta` (se calculan automáticamente)
    /// - ❌ NO enviar `dmsOrigen` (se consulta de CO_DISTRIBUIDORES automáticamente)
    /// - ❌ NO enviar `dealerBac` (se toma del JWT token)
    /// - ❌ NO enviar `nombreDealer` (se consulta de CO_DISTRIBUIDORES automáticamente)
    /// - ✅ La combinación `cargaArchivoSincronizacionId` + `dealerBac` debe ser única
    /// - ✅ El `cargaArchivoSincronizacionId` debe existir en `CO_CARGAARCHIVOSINCRONIZACION` y estar activo (`COCA_ACTUAL=1`)
    /// - ✅ El `dealerBac` del JWT debe existir en `CO_DISTRIBUIDORES`
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Registro de sincronización creado con todos sus campos
    /// - ID generado automáticamente
    /// - Token de confirmación (hash SHA256) generado automáticamente
    /// - Timestamp de la operación
    /// - Datos del dealer consultados de `CO_DISTRIBUIDORES` (dmsOrigen, nombreDealer)
    /// </remarks>
    /// <param name="dto">Datos del nuevo registro de sincronización (solo cargaArchivoSincronizacionId)</param>
    /// <returns>Registro de sincronización creado con todos los campos calculados automáticamente, incluyendo proceso y registrosSincronizados obtenidos de CO_CARGAARCHIVOSINCRONIZACION</returns>
    /// <response code="201">Registro creado exitosamente.</response>
    /// <response code="400">Error de validación en los datos enviados o registro duplicado.</response>
    /// <response code="401">No autorizado si no se proporciona un token JWT válido.</response>
    /// <response code="404">No se encontró el dealer en CO_DISTRIBUIDORES.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SincArchivoDealerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Crear([FromBody] CrearSincArchivoDealerDto dto)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var dealerBac = JwtUserHelper.GetDealerBac(User, _logger);
        var usuarioAlta = JwtUserHelper.GetCurrentUser(User, _logger);
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "🔷 [CONTROLLER] Creando registro de sincronización. DealerBac: {DealerBac}, CargaId: {CargaId}, CorrelationId: {CorrelationId}",
            dealerBac, dto.CargaArchivoSincronizacionId, correlationId);

        try
        {
            var resultado = await _sincArchivoDealerService.CrearAsync(dto, dealerBac, usuarioAlta);

            stopwatch.Stop();
            _logger.LogInformation(
                "✅ [CONTROLLER] Registro creado exitosamente. DealerBac: {DealerBac}, SincId: {SincId}, Tiempo: {ElapsedMs}ms",
                dealerBac, resultado.SincArchivoDealerId, stopwatch.ElapsedMilliseconds);

            return CreatedAtAction(
                nameof(Crear),
                new { id = resultado.SincArchivoDealerId },
                new ApiResponse<SincArchivoDealerDto>
                {
                    Success = true,
                    Message = "Registro de sincronización creado exitosamente",
                    Data = resultado,
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
        }
        catch (NotFoundException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "⚠️ [CONTROLLER] Dealer no encontrado. DealerBac: {DealerBac}, Tiempo: {ElapsedMs}ms",
                dealerBac, stopwatch.ElapsedMilliseconds);

            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (BusinessValidationException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "⚠️ [CONTROLLER] Error de validación. DealerBac: {DealerBac}, Tiempo: {ElapsedMs}ms",
                dealerBac, stopwatch.ElapsedMilliseconds);

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "❌ [CONTROLLER] Error inesperado. DealerBac: {DealerBac}, Tiempo: {ElapsedMs}ms",
                dealerBac, stopwatch.ElapsedMilliseconds);

            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error interno del servidor",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}

