using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Exceptions;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.FotoDealersCargaArchivosSinc;

/// <summary>
/// Controller para creación batch de fotos de dealers carga archivos sincronización.
/// Ruta base: /api/v1/gm/catalog-sync/foto-dealers-carga-archivos-sinc
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/foto-dealers-carga-archivos-sinc")]
[Produces("application/json")]
[Authorize]
public class CreateFotoDealersCargaArchivosSincController : ControllerBase
{
    private readonly IFotoDealersCargaArchivosSincService _service;
    private readonly ILogger<CreateFotoDealersCargaArchivosSincController> _logger;

    public CreateFotoDealersCargaArchivosSincController(
        IFotoDealersCargaArchivosSincService service,
        ILogger<CreateFotoDealersCargaArchivosSincController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Crea múltiples registros de fotos de dealers carga archivos sincronización en batch
    /// </summary>
    /// <remarks>
    /// Este endpoint permite crear múltiples registros de fotos de dealers carga archivos sincronización en una sola operación (batch insert).
    /// **Los distribuidores se generan automáticamente desde CO_DISTRIBUIDORES** basándose en el empresaId y usuario del JWT token.
    /// 
    /// **Este endpoint NO recibe payload en el body. El `cargaArchivoSincronizacionId` se pasa como parámetro de ruta.**
    /// 
    /// **Funcionalidad automática:**
    /// - Los distribuidores se obtienen automáticamente desde `CO_DISTRIBUIDORES`
    /// - Se filtran por `EMPR_EMPRESAID` (obtenido del JWT token)
    /// - Si el usuario está en el JWT, se filtran también por `CO_USUARIOXDEALER` (solo distribuidores asociados al usuario)
    /// - Se genera un registro automáticamente para cada distribuidor encontrado
    /// - Los registros que ya existen se omiten automáticamente (no se insertan duplicados)
    /// 
    /// **Funcionalidad de la transacción:**
    /// - Todos los registros se insertan en una sola transacción
    /// - Si falla cualquier registro, se hace ROLLBACK completo de todos los registros
    /// - Garantiza integridad de datos (todo o nada)
    /// 
    /// **Validaciones:**
    /// - El `cargaArchivoSincronizacionId` debe existir en `CO_CARGAARCHIVOSINCRONIZACION`
    /// - Se valida la llave única (COFD_COCA_CARGAARCHIVOSINID, COSA_DEALERBAC) ANTES de insertar
    /// - Los registros duplicados se omiten automáticamente (no se insertan)
    /// - Si todos los distribuidores ya tienen registro, retorna error sin insertar
    /// 
    /// **Parámetros obligatorios:**
    /// - `cargaArchivoSincronizacionId`: ID de la carga de archivo de sincronización (FK) - se pasa como parámetro de ruta
    /// 
    /// **Campos generados automáticamente desde CO_DISTRIBUIDORES:**
    /// - `dealerBac`: Se obtiene de `CO_DISTRIBUIDORES.DEALERID`
    /// - `nombreDealer`: Se obtiene de `CO_DISTRIBUIDORES.CODI_NOMBRE`
    /// - `razonSocialDealer`: Se obtiene de `CO_DISTRIBUIDORES.CODI_RAZONSOCIAL`
    /// - `dms`: Se obtiene de `CO_DISTRIBUIDORES.CODI_DMS` (si está vacío, se usa "GDMS" por defecto)
    /// 
    /// **Campos calculados automáticamente:**
    /// - `fechaRegistro`: Se calcula automáticamente con hora de México
    /// - `fotoDealersCargaArchivosSincId`: ID único generado por secuencia de Oracle
    /// - `fechaAlta`: Fecha y hora del servidor Oracle (SYSDATE)
    /// - `usuarioAlta`: Se toma del JWT token
    /// 
    /// **Ejemplo de uso:**
    /// ```
    /// POST /api/v1/gm/catalog-sync/foto-dealers-carga-archivos-sinc/15
    /// ```
    /// 
    /// ⚠️ **IMPORTANTE:**
    /// - ❌ NO se envía payload en el body (el endpoint no recibe JSON)
    /// - ❌ NO enviar `dealerBac`, `nombreDealer`, `razonSocialDealer`, `dms` (se obtienen automáticamente de CO_DISTRIBUIDORES)
    /// - ❌ NO enviar `fotoDealersCargaArchivosSincId` (se genera automáticamente)
    /// - ❌ NO enviar `fechaRegistro` (se calcula automáticamente con hora de México)
    /// - ❌ NO enviar campos de auditoría (se calculan automáticamente)
    /// - ❌ NO enviar `usuarioAlta` (se toma del JWT token)
    /// - ✅ Solo se requiere `cargaArchivoSincronizacionId` como parámetro de ruta
    /// - ✅ Los distribuidores se filtran automáticamente por empresaId del JWT
    /// - ✅ Si el usuario está en el JWT, solo se incluyen distribuidores asociados al usuario
    /// - ✅ La combinación (cargaArchivoSincronizacionId, dealerBac) debe ser única
    /// - ✅ Todos los registros se procesan en una sola transacción
    /// - ✅ Los registros duplicados se omiten automáticamente (no se insertan)
    /// 
    /// **Proceso interno:**
    /// 1. Validar que existe `cargaArchivoSincronizacionId` en `CO_CARGAARCHIVOSINCRONIZACION`
    /// 2. Obtener empresaId y usuario del JWT token
    /// 3. Consultar todos los distribuidores desde `CO_DISTRIBUIDORES` filtrados por empresaId (y usuario si está disponible)
    /// 4. Generar registros automáticamente para cada distribuidor encontrado
    /// 5. Validar duplicados en BD (query SELECT COUNT) - ANTES de insertar
    /// 6. Filtrar solo los registros nuevos (omitir duplicados)
    /// 7. Si no hay registros nuevos, retornar error
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
    /// <param name="cargaArchivoSincronizacionId">ID de la carga de archivo de sincronización (FK) - se pasa como parámetro de ruta</param>
    /// <returns>Lista de registros creados</returns>
    /// <response code="201">Registros creados exitosamente.</response>
    /// <response code="400">Error de validación en los datos enviados.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost("{cargaArchivoSincronizacionId}")]
    [ProducesResponseType(typeof(ApiResponse<List<FotoDealersCargaArchivosSincDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CrearBatch([FromRoute] int cargaArchivoSincronizacionId)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var userId = JwtUserHelper.GetCurrentUser(User, _logger);

        _logger.LogInformation(
            "[{CorrelationId}] 📝 Inicio POST /foto-dealers-carga-archivos-sinc/{CargaId} (batch). Usuario: {UserId}",
            correlationId, cargaArchivoSincronizacionId, userId);

        try
        {
            // Validar que el cargaArchivoSincronizacionId sea válido
            if (cargaArchivoSincronizacionId <= 0)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ cargaArchivoSincronizacionId inválido: {CargaId}",
                    correlationId, cargaArchivoSincronizacionId);

                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "El cargaArchivoSincronizacionId debe ser mayor a 0",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            // Obtener empresaId y usuario del JWT para filtrar distribuidores
            var empresaId = JwtUserHelper.GetEmpresaId(User, _logger);
            var usuario = JwtUserHelper.GetCurrentUser(User, _logger);

            // Crear DTO con el cargaArchivoSincronizacionId
            var dto = new CrearFotoDealersCargaArchivosSincBatchDto
            {
                CargaArchivoSincronizacionId = cargaArchivoSincronizacionId
            };

            var resultados = await _service.CrearBatchAsync(dto, userId, empresaId, usuario);

            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] ✅ POST /foto-dealers-carga-archivos-sinc (batch) completado en {ElapsedMs}ms. {Cantidad} registros creados",
                correlationId, stopwatch.ElapsedMilliseconds, resultados.Count);

            return CreatedAtAction(
                nameof(GetFotoDealersCargaArchivosSincController.ObtenerTodos),
                "GetFotoDealersCargaArchivosSinc",
                null,
                new ApiResponse<List<FotoDealersCargaArchivosSincDto>>
                {
                    Success = true,
                    Message = $"Se crearon exitosamente {resultados.Count} registros de fotos de dealers carga archivos sincronización",
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

            // Mapear ValidationError a ErrorDetail
            var errorDetails = ex.Errors?.Select(err => new ErrorDetail
            {
                Code = err.Field.Contains("cargaArchivoSincronizacionId") ? "CARGA_NOT_FOUND" : "DUPLICATE_RECORD",
                Field = err.Field,
                Message = err.Message,
                Details = new Dictionary<string, object>
                {
                    { "attemptedValue", err.AttemptedValue ?? "N/A" },
                    { "constraint", err.Field.Contains("cargaArchivoSincronizacionId") ? "FK_COFD_COCA_CARGAARCHIVOSINID" : "UQ_COFD_CARGA_DEALER" },
                    { "suggestion", err.Field.Contains("cargaArchivoSincronizacionId") 
                        ? "Verifique que el cargaArchivoSincronizacionId existe en CO_CARGAARCHIVOSINCRONIZACION" 
                        : "La combinación de cargaArchivoSincronizacionId y dealerBac debe ser única. Verifique si ya existe un registro con estos valores." }
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
                "[{CorrelationId}] ❌ Error en POST /foto-dealers-carga-archivos-sinc (batch). Tiempo: {ElapsedMs}ms",
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

