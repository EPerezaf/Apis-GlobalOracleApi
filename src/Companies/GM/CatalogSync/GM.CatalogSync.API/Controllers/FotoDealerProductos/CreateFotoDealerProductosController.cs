using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Exceptions;
using Shared.Security;

namespace GM.CatalogSync.API.Controllers.FotoDealerProductos;

/// <summary>
/// Controller para creación batch de fotos de dealer productos.
/// Ruta base: /api/v1/gm/catalog-sync/foto-dealer-productos
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/foto-dealer-productos")]
[Produces("application/json")]
[Authorize]
public class CreateFotoDealerProductosController : ControllerBase
{
    private readonly IFotoDealerProductosService _service;
    private readonly ILogger<CreateFotoDealerProductosController> _logger;

    public CreateFotoDealerProductosController(
        IFotoDealerProductosService service,
        ILogger<CreateFotoDealerProductosController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Crea múltiples registros de fotos de dealer productos en batch
    /// </summary>
    /// <remarks>
    /// Este endpoint permite crear múltiples registros de fotos de dealer productos en una sola operación (batch insert).
    /// 
    /// **Funcionalidad de la transacción:**
    /// - Todos los registros se insertan en una sola transacción
    /// - Si falla cualquier registro, se hace ROLLBACK completo de todos los registros
    /// - Garantiza integridad de datos (todo o nada)
    /// 
    /// **Validaciones:**
    /// - El campo 'json' no puede estar vacío
    /// - Cada registro debe cumplir con las validaciones de `CrearFotoDealerProductosDto`
    /// - Se valida la llave única (COFD_COCA_CARGAARCHIVOSINID, COSA_DEALERBAC) ANTES de insertar
    /// - Si hay duplicados, retorna error sin insertar ningún registro
    /// 
    /// **Campos obligatorios por registro:**
    /// - `cargaArchivoSincronizacionId`: ID de la carga de archivo de sincronización (FK)
    /// - `dealerBac`: Código BAC del dealer (FK)
    /// - `nombreDealer`: Nombre comercial del dealer
    /// - `razonSocialDealer`: Razón social legal del dealer
    /// - `dms`: Sistema DMS utilizado
    /// 
    /// **Campos calculados automáticamente (NO enviar en el request):**
    /// - `fechaRegistro`: Se calcula automáticamente con hora de México
    /// - `fotoDealerProductosId`: ID único generado por secuencia de Oracle
    /// - `fechaAlta`: Fecha y hora del servidor Oracle (SYSDATE)
    /// - `usuarioAlta`: Se toma del JWT token
    /// 
    /// **Formato del Request:**
    /// ```json
    /// {
    ///   "json": [
    ///     {
    ///       "cargaArchivoSincronizacionId": 1,
    ///       "dealerBac": "DEALER001",
    ///       "nombreDealer": "Dealer ABC",
    ///       "razonSocialDealer": "ABC Automotriz S.A. de C.V.",
    ///       "dms": "CDK"
    ///     },
    ///     {
    ///       "cargaArchivoSincronizacionId": 1,
    ///       "dealerBac": "DEALER002",
    ///       "nombreDealer": "Dealer XYZ",
    ///       "razonSocialDealer": "XYZ Automotriz S.A. de C.V.",
    ///       "dms": "Reynolds"
    ///     }
    ///   ]
    /// }
    /// ```
    /// 
    /// ⚠️ **IMPORTANTE:**
    /// - ❌ NO enviar `fotoDealerProductosId` (se genera automáticamente)
    /// - ❌ NO enviar `fechaRegistro` (se calcula automáticamente con hora de México)
    /// - ❌ NO enviar campos de auditoría (se calculan automáticamente)
    /// - ❌ NO enviar `usuarioAlta` (se toma del JWT token)
    /// - ✅ La combinación (cargaArchivoSincronizacionId, dealerBac) debe ser única
    /// - ✅ Todos los registros se procesan en una sola transacción
    /// - ✅ Validación previa: verifica duplicados ANTES de insertar
    /// 
    /// **Proceso interno:**
    /// 1. Validar que el campo 'json' no esté vacío
    /// 2. Validar duplicados dentro del mismo batch
    /// 3. Validar duplicados en BD (query SELECT COUNT) - ANTES de insertar
    /// 4. Si hay duplicados, retornar error sin insertar
    /// 5. Si todo OK, iniciar transacción
    /// 6. INSERT: Crear todos los registros en batch
    /// 7. COMMIT o ROLLBACK según resultado
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Lista de registros creados con todos sus campos
    /// - IDs generados automáticamente
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="dto">DTO con la lista de registros a crear</param>
    /// <returns>Lista de registros creados</returns>
    /// <response code="201">Registros creados exitosamente.</response>
    /// <response code="400">Error de validación en los datos enviados.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<List<FotoDealerProductosDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CrearBatch([FromBody] CrearFotoDealerProductosBatchDto dto)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var userId = JwtUserHelper.GetCurrentUser(User, _logger);

        _logger.LogInformation(
            "[{CorrelationId}] 📝 Inicio POST /foto-dealer-productos (batch). Usuario: {UserId}, Cantidad: {Cantidad}",
            correlationId, userId, dto?.Json?.Count ?? 0);

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

            if (dto == null || dto.Json == null || !dto.Json.Any())
            {
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ Campo 'json' vacío o nulo",
                    correlationId);

                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "El campo 'json' no puede estar vacío",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            var resultados = await _service.CrearBatchAsync(dto, userId);

            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] ✅ POST /foto-dealer-productos (batch) completado en {ElapsedMs}ms. {Cantidad} registros creados",
                correlationId, stopwatch.ElapsedMilliseconds, resultados.Count);

            return CreatedAtAction(
                nameof(GetFotoDealerProductosController.ObtenerTodos),
                "GetFotoDealerProductos",
                null,
                new ApiResponse<List<FotoDealerProductosDto>>
                {
                    Success = true,
                    Message = $"Se crearon exitosamente {resultados.Count} registros de fotos de dealer productos",
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
                "[{CorrelationId}] ❌ Error en POST /foto-dealer-productos (batch). Tiempo: {ElapsedMs}ms",
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

