using GM.DealersSincronizacion.Application.DTOs;
using GM.DealersSincronizacion.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;
using System.Diagnostics;

namespace GM.DealersSincronizacion.API.Controllers;

/// <summary>
/// Controller para obtener el registro actual de carga de archivo de sincronización.
/// </summary>
[ApiController]
[Route("api/v1/gm/dealer-sinc/carga-archivos-sinc-actual")]
[Authorize]
public class GetCargaArchivosSincController : ControllerBase
{
    private readonly ICargaArchivoSincService _cargaArchivoSincService;
    private readonly ILogger<GetCargaArchivosSincController> _logger;

    public GetCargaArchivosSincController(
        ICargaArchivoSincService cargaArchivoSincService,
        ILogger<GetCargaArchivosSincController> logger)
    {
        _cargaArchivoSincService = cargaArchivoSincService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el registro actual (actual=true) de carga de archivo de sincronización.
    /// </summary>
    /// <remarks>
    /// Este endpoint retorna el registro de carga de archivo de sincronización que está marcado como actual (`COCA_ACTUAL=1`)
    /// desde la tabla `CO_CARGAARCHIVOSINCRONIZACION`. El dealerBac se obtiene automáticamente del token JWT para futuras validaciones o filtros.
    /// 
    /// **Funcionalidad:**
    /// - Consulta el registro de carga de archivo de sincronización con `COCA_ACTUAL=1` y `COCA_PROCESO` igual al proceso especificado
    /// - Filtra por el proceso específico proporcionado (ej: "ProductList")
    /// - Retorna información esencial del registro actual para que el dealer pueda sincronizar
    /// - Los campos de dealers (dealersTotales, dealersSincronizados, porcDealersSinc) NO se exponen a los dealers
    /// 
    /// **Parámetros obligatorios:**
    /// - `proceso`: Nombre del proceso de sincronización para filtrar (obligatorio, ej: "ProductList")
    ///   - Retorna solo el registro actual que coincida con ese proceso específico
    ///   - Debe existir un registro con `COCA_ACTUAL=1` y `COCA_PROCESO` igual al proceso especificado
    /// 
    /// **Ejemplos de uso:**
    /// - GET /api/v1/gm/dealer-sinc/carga-archivos-sinc-actual?proceso=ProductList
    /// 
    /// **Campos en la respuesta:**
    /// - `cargaArchivoSincronizacionId`: ID único del registro de carga (PK)
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
    /// - Debe existir un registro con `COCA_ACTUAL=1` y `COCA_PROCESO` igual al proceso especificado
    /// - Si no existe registro actual con el proceso especificado, retorna error 404 Not Found
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Registro actual de carga de archivo de sincronización
    /// - Información necesaria para realizar la sincronización
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="proceso">Nombre del proceso de sincronización para filtrar (obligatorio, ej: "ProductList")</param>
    /// <returns>Registro actual de carga de archivo de sincronización</returns>
    /// <response code="200">Operación exitosa. Retorna el registro actual de carga.</response>
    /// <response code="400">Error de validación si no se proporciona el parámetro proceso.</response>
    /// <response code="401">No autorizado si no se proporciona un token JWT válido.</response>
    /// <response code="404">No se encontró registro actual de carga de archivo de sincronización para el proceso especificado.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<CargaArchivoSincActualDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 401)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
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
            "🔷 [CONTROLLER] Obteniendo registro actual de carga. DealerBac: {DealerBac}, Proceso: {Proceso}, CorrelationId: {CorrelationId}",
            dealerBac, proceso, correlationId);

        try
        {
            var carga = await _cargaArchivoSincService.ObtenerActualPorProcesoAsync(proceso.Trim());

            stopwatch.Stop();

            if (carga == null)
            {
                _logger.LogWarning("⚠️ [CONTROLLER] No se encontró registro actual de carga para proceso. DealerBac: {DealerBac}, Proceso: {Proceso}", 
                    dealerBac, proceso);
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"No se encontró registro actual de carga de archivo de sincronización para el proceso '{proceso}'",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            _logger.LogInformation(
                "✅ [CONTROLLER] Registro actual obtenido. DealerBac: {DealerBac}, CargaId: {CargaId}, Tiempo: {ElapsedMs}ms",
                dealerBac, carga.CargaArchivoSincronizacionId, stopwatch.ElapsedMilliseconds);

            return Ok(new ApiResponse<CargaArchivoSincActualDto>
            {
                Success = true,
                Message = "Registro actual obtenido exitosamente",
                Data = carga,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "❌ [CONTROLLER] Error al obtener registro actual. DealerBac: {DealerBac}", dealerBac);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error interno del servidor",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}

