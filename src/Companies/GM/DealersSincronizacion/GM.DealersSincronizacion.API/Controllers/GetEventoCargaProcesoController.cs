using System.Diagnostics;
using GM.DealersSincronizacion.Application.DTOs;
using GM.DealersSincronizacion.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;

namespace GM.DealersSincronizacion.API.Controllers;

/// <summary>
/// Controller para obtener el registro actual de evento de carga de proceso.
/// Ruta base: /api/v1/gm/dealer-sinc/evento-carga-proceso
/// </summary>
[ApiController]
[Route("api/v1/gm/dealer-sinc/evento-carga-proceso")]
[Produces("application/json")]
[Authorize]
public class GetEventoCargaProcesoController : ControllerBase
{
    private readonly IEventoCargaProcesoService _service;
    private readonly ILogger<GetEventoCargaProcesoController> _logger;

    public GetEventoCargaProcesoController(
        IEventoCargaProcesoService service,
        ILogger<GetEventoCargaProcesoController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el registro actual (actual=true) de evento de carga de proceso filtrado por proceso.
    /// </summary>
    /// <remarks>
    /// Este endpoint retorna el registro de evento de carga de proceso que está marcado como actual (`COCP_ACTUAL=1`)
    /// desde la tabla `CO_EVENTOSCARGAPROCESO`, filtrado por el proceso especificado.
    /// 
    /// **Funcionalidad:**
    /// - Consulta el registro de evento de carga de proceso con `COCP_ACTUAL=1` y `COCP_PROCESO` igual al proceso especificado
    /// - Filtra por el proceso específico proporcionado (ej: "ProductList")
    /// - Retorna información esencial del registro actual para que el dealer pueda sincronizar
    /// - Los campos de dealers (dealersTotales, dealersSincronizados, porcDealersSinc) NO se exponen a los dealers
    /// 
    /// **Parámetros obligatorios:**
    /// - `proceso`: Nombre del proceso de sincronización para filtrar (obligatorio, ej: "ProductList")
    ///   - Retorna solo el registro actual que coincida con ese proceso específico
    ///   - Debe existir un registro con `COCP_ACTUAL=1` y `COCP_PROCESO` igual al proceso especificado
    /// 
    /// **Ejemplos de uso:**
    /// - GET /api/v1/gm/dealer-sinc/evento-carga-proceso/actual?proceso=ProductList
    /// 
    /// **Campos en la respuesta:**
    /// - `eventoCargaProcesoId`: ID único del registro de evento de carga (PK)
    /// - `proceso`: Nombre del proceso de sincronización (ej: "ProductList")
    /// - `nombreArchivo`: Nombre del archivo cargado (ej: "catalogo_productos_23122025.xlsx")
    /// - `fechaCarga`: Fecha y hora en que se realizó la carga del archivo
    /// - `idCarga`: Identificador único de la carga (ej: "catalogo_productos_23122025_1359")
    /// - `registros`: Número total de registros procesados en la carga
    /// - `actual`: Indica si es la carga actual (siempre true en esta respuesta)
    /// - `tablaRelacion`: Nombre de la tabla relacionada (opcional, ej: "CO_GM_LISTAPRODUCTOS")
    /// 
    /// **Campos NO expuestos a dealers:**
    /// - `dealersTotales`: No se incluye en la respuesta (información interna)
    /// - `dealersSincronizados`: No se incluye en la respuesta (información interna)
    /// - `porcDealersSinc`: No se incluye en la respuesta (información interna)
    /// 
    /// **Validaciones:**
    /// - El usuario debe estar autenticado (JWT requerido)
    /// - El dealerBac se obtiene automáticamente del token JWT
    /// - El parámetro `proceso` es obligatorio
    /// - Debe existir un registro con `COCP_ACTUAL=1` y `COCP_PROCESO` igual al proceso especificado
    /// - Si no existe registro actual con el proceso especificado, retorna error 404 Not Found
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Registro actual de evento de carga de proceso
    /// - Información necesaria para realizar la sincronización
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="proceso">Nombre del proceso de sincronización para filtrar (obligatorio, ej: "ProductList")</param>
    /// <returns>Registro actual de evento de carga de proceso</returns>
    /// <response code="200">Operación exitosa. Retorna el registro actual de evento de carga.</response>
    /// <response code="400">Error de validación si no se proporciona el parámetro proceso.</response>
    /// <response code="401">No autorizado si no se proporciona un token JWT válido.</response>
    /// <response code="404">No se encontró registro actual de evento de carga de proceso para el proceso especificado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet("actual")]
    [ProducesResponseType(typeof(ApiResponse<EventoCargaProcesoActualDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ObtenerActual([FromQuery] string proceso)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var dealerBac = JwtUserHelper.GetDealerBac(User, _logger);
        var stopwatch = Stopwatch.StartNew();

        // Validar que el proceso sea obligatorio
        if (string.IsNullOrWhiteSpace(proceso))
        {
            _logger.LogWarning("⚠️ [CONTROLLER] Parámetro 'proceso' es obligatorio. DealerBac: {DealerBac}", dealerBac);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "El parámetro 'proceso' es obligatorio (ej: 'ProductList')",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }

        _logger.LogInformation(
            "[{CorrelationId}] 🔷 Inicio GET /evento-carga-proceso/actual. DealerBac: {DealerBac}, Proceso: {Proceso}",
            correlationId, dealerBac, proceso);

        try
        {
            var evento = await _service.ObtenerActualPorProcesoAsync(proceso.Trim());

            stopwatch.Stop();

            if (evento == null)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ No se encontró registro actual de evento de carga para proceso. DealerBac: {DealerBac}, Proceso: {Proceso}, Tiempo: {ElapsedMs}ms",
                    correlationId, dealerBac, proceso, stopwatch.ElapsedMilliseconds);
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"No se encontró registro actual de evento de carga de proceso para el proceso '{proceso}'",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            _logger.LogInformation(
                "[{CorrelationId}] ✅ GET /evento-carga-proceso/actual completado. DealerBac: {DealerBac}, EventoCargaProcesoId: {EventoCargaProcesoId}, Tiempo: {ElapsedMs}ms",
                correlationId, dealerBac, evento.EventoCargaProcesoId, stopwatch.ElapsedMilliseconds);

            return Ok(new ApiResponse<EventoCargaProcesoActualDto>
            {
                Success = true,
                Message = "Registro actual obtenido exitosamente",
                Data = evento,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "[{CorrelationId}] ❌ Error en GET /evento-carga-proceso/actual. DealerBac: {DealerBac}, Tiempo: {ElapsedMs}ms",
                correlationId, dealerBac, stopwatch.ElapsedMilliseconds);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error interno del servidor",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}

