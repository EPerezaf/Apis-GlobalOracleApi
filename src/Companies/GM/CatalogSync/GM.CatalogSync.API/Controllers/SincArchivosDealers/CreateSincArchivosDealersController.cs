using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
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
    /// **Validaciones:**
    /// - La combinación de `proceso`, `idCarga` y `dealerBac` debe ser única (constraint UQ_COSA_PROCESO_CARGA_DEALER)
    /// - Si ya existe un registro con la misma combinación, retorna error 409 Conflict
    /// 
    /// **Campos obligatorios:**
    /// - `proceso`: Nombre del proceso de sincronización (ej: "ProductsCatalog")
    /// - `idCarga`: ID de la carga relacionada (ej: "products_catalog_16122025_1335")
    /// - `dmsOrigen`: Sistema DMS origen (ej: "Reynolds", "CDK")
    /// - `dealerBac`: Código BAC del dealer (ej: "MX001")
    /// - `nombreDealer`: Nombre del dealer (ej: "Chevrolet Polanco")
    /// - `registrosSincronizados`: Cantidad de registros sincronizados (ej: 150)
    /// 
    /// **Campos calculados automáticamente:**
    /// - `fechaSincronizacion`: Fecha de sincronización (SYSDATE)
    /// - `sincArchivoDealerId`: ID único generado por secuencia
    /// - `fechaAlta`: Fecha y hora del servidor (SYSDATE)
    /// - `usuarioAlta`: Usuario autenticado (JWT)
    /// - `fechaModificacion`: null (no aplica en creación)
    /// - `usuarioModificacion`: null (no aplica en creación)
    /// 
    /// **Formato del Request:**
    /// ```json
    /// {
    ///   "proceso": "ProductsCatalog",
    ///   "idCarga": "products_catalog_16122025_1335",
    ///   "dmsOrigen": "Reynolds",
    ///   "dealerBac": "MX001",
    ///   "nombreDealer": "Chevrolet Polanco",
    ///   "registrosSincronizados": 150
    /// }
    /// ```
    /// 
    /// ⚠️ **IMPORTANTE:**
    /// - ❌ NO enviar `sincArchivoDealerId` (se genera automáticamente)
    /// - ❌ NO enviar campos de auditoría (se calculan automáticamente)
    /// - ✅ La combinación proceso + idCarga + dealerBac debe ser única
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Registro de sincronización creado con todos sus campos
    /// - ID generado automáticamente
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="dto">Datos del nuevo registro de sincronización</param>
    /// <returns>Registro de sincronización creado</returns>
    /// <response code="201">Registro creado exitosamente.</response>
    /// <response code="400">Error de validación en los datos enviados.</response>
    /// <response code="409">Ya existe un registro con la misma combinación proceso/idCarga/dealerBac (duplicado).</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost]
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
                var errores = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ Validación fallida. Errores: {@Errores}",
                    correlationId, errores);

                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = string.Join("; ", errores),
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            var resultado = await _service.CrearAsync(dto, userId);

            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] ✅ POST /sinc-archivos-dealers completado en {ElapsedMs}ms. ID: {Id}, Proceso: {Proceso}, DealerBac: {DealerBac}",
                correlationId, stopwatch.ElapsedMilliseconds, resultado.SincArchivoDealerId, resultado.Proceso, resultado.DealerBac);

            return CreatedAtAction(
                nameof(GetSincArchivosDealersController.ObtenerPorId),
                "GetSincArchivosDealers",
                new { id = resultado.SincArchivoDealerId },
                new ApiResponse<SincArchivoDealerDto>
                {
                    Success = true,
                    Message = "Registro de sincronización creado exitosamente",
                    Data = resultado,
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
        }
        catch (SincArchivoDealerDuplicadoException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "[{CorrelationId}] ⚠️ Registro duplicado - Proceso: {Proceso}, IdCarga: {IdCarga}, DealerBac: {DealerBac}. Tiempo: {ElapsedMs}ms",
                correlationId, ex.Proceso, ex.IdCarga, ex.DealerBac, stopwatch.ElapsedMilliseconds);

            return Conflict(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
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

