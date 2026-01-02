using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.EventoCargaProceso;

/// <summary>
/// Controller para creación de eventos de carga de proceso.
/// Ruta base: /api/v1/gm/catalog-sync/evento-carga-proceso
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/evento-carga-proceso")]
[Produces("application/json")]
[Authorize]
public class CreateEventoCargaProcesoController : ControllerBase
{
    private readonly IEventoCargaProcesoService _service;
    private readonly ILogger<CreateEventoCargaProcesoController> _logger;

    public CreateEventoCargaProcesoController(
        IEventoCargaProcesoService service,
        ILogger<CreateEventoCargaProcesoController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Crea un nuevo registro de evento de carga de proceso
    /// </summary>
    /// <remarks>
    /// Este endpoint permite registrar un nuevo evento de carga de proceso.
    /// 
    /// **Funcionalidad de la transacción:**
    /// - Automáticamente marca los registros anteriores del mismo proceso como NO actuales (COCP_ACTUAL = 0)
    /// - El nuevo registro se crea como ACTUAL (COCP_ACTUAL = 1)
    /// - Si falla cualquier operación, se hace ROLLBACK completo
    /// 
    /// **Validaciones:**
    /// - El campo `idCarga` debe ser único en toda la tabla
    /// - Si ya existe un registro con el mismo `idCarga`, retorna error 409 Conflict
    /// 
    /// **Campos obligatorios:**
    /// - `proceso`: Nombre del proceso de sincronización (ej: "ProductsCatalog")
    /// - `nombreArchivo`: Nombre del archivo cargado (ej: "products_gm_list.xlsx")
    /// - `idCarga`: Identificador único de la carga (ej: "products_catalog_16122025_1335")
    /// - `registros`: Cantidad de registros procesados (ej: 520)
    /// - `dealersTotales`: Número total de dealers a sincronizar (ej: 150)
    /// 
    /// **Campos opcionales:**
    /// - `tablaRelacion`: Nombre de la tabla relacionada (ej: "CO_PRODUCTOS")
    /// - `componenteRelacionado`: Componente relacionado (ej: "CatalogSync")
    /// 
    /// **Campos calculados automáticamente (NO enviar en el request):**
    /// - `eventoCargaProcesoId`: ID único generado por secuencia de Oracle
    /// - `fechaCarga`: Se calcula automáticamente con hora de México
    /// - `actual`: Siempre se establece en true (1) para el nuevo registro
    /// - `fechaAlta`: Fecha y hora del servidor Oracle (SYSDATE)
    /// - `usuarioAlta`: Se toma del JWT token
    /// 
    /// **Formato del Request:**
    /// ```json
    /// {
    ///   "proceso": "ProductsCatalog",
    ///   "nombreArchivo": "products_gm_list.xlsx",
    ///   "idCarga": "products_catalog_16122025_1335",
    ///   "registros": 520,
    ///   "dealersTotales": 150,
    ///   "tablaRelacion": "CO_PRODUCTOS",
    ///   "componenteRelacionado": "CatalogSync"
    /// }
    /// ```
    /// 
    /// ⚠️ **IMPORTANTE:**
    /// - ❌ NO enviar `eventoCargaProcesoId` (se genera automáticamente)
    /// - ❌ NO enviar `fechaCarga` (se calcula automáticamente con hora de México)
    /// - ❌ NO enviar `actual` (siempre es true para nuevos registros)
    /// - ❌ NO enviar `fechaAlta`, `usuarioAlta` (se calculan automáticamente)
    /// - ✅ El `idCarga` debe ser único y descriptivo
    /// 
    /// **Proceso interno:**
    /// 1. Validar que `idCarga` no exista en la base de datos
    /// 2. Calcular automáticamente `fechaCarga` con hora de México
    /// 3. Iniciar transacción
    /// 4. UPDATE: Marcar registros anteriores del mismo proceso como COCP_ACTUAL = 0
    /// 5. INSERT: Crear nuevo registro con COCP_ACTUAL = 1
    /// 6. COMMIT o ROLLBACK según resultado
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Registro de evento de carga creado con todos sus campos
    /// - ID generado automáticamente
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="dto">Datos del nuevo registro de evento de carga de proceso</param>
    /// <returns>Registro de evento de carga creado</returns>
    /// <response code="201">Registro creado exitosamente.</response>
    /// <response code="400">Error de validación en los datos enviados.</response>
    /// <response code="409">Ya existe un registro con el mismo idCarga (duplicado).</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EventoCargaProcesoDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Crear([FromBody] CrearEventoCargaProcesoDto dto)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var userId = JwtUserHelper.GetCurrentUser(User, _logger);

        _logger.LogInformation(
            "[{CorrelationId}] 📝 Inicio POST /evento-carga-proceso. Usuario: {UserId}, Request: {@Request}",
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
                "[{CorrelationId}] ✅ POST /evento-carga-proceso completado en {ElapsedMs}ms. ID: {Id}, Proceso: {Proceso}",
                correlationId, stopwatch.ElapsedMilliseconds, resultado.EventoCargaProcesoId, resultado.Proceso);

            return CreatedAtAction(
                nameof(GetEventoCargaProcesoController.ObtenerPorId),
                "GetEventoCargaProceso",
                new { id = resultado.EventoCargaProcesoId },
                new ApiResponse<EventoCargaProcesoDto>
                {
                    Success = true,
                    Message = "Registro de evento de carga de proceso creado exitosamente",
                    Data = resultado,
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
        }
        catch (IdCargaDuplicadoException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "[{CorrelationId}] ⚠️ ID de carga duplicado: {IdCarga}. Tiempo: {ElapsedMs}ms",
                correlationId, ex.IdCarga, stopwatch.ElapsedMilliseconds);

            return Conflict(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (CargaArchivoSincValidacionException ex)
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
                "[{CorrelationId}] ❌ Error en POST /evento-carga-proceso. Tiempo: {ElapsedMs}ms",
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

