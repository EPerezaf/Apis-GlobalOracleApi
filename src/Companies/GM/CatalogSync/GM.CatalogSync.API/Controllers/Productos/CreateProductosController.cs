using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;
using System.Diagnostics;

namespace GM.CatalogSync.API.Controllers.Productos;

/// <summary>
/// Controller para operaciones POST de productos
/// </summary>
[ApiController]
[Route("api/v1/gm/catalog-sync/productos")]
[Produces("application/json")]
[Authorize]
public class CreateProductosController : ControllerBase
{
    private readonly IProductoService _service;
    private readonly ILogger<CreateProductosController> _logger;

    public CreateProductosController(
        IProductoService service,
        ILogger<CreateProductosController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Crea productos en el listado (operación batch INSERT)
    /// </summary>
    /// <remarks>
    /// Este endpoint permite crear múltiples productos en una sola operación batch (INSERT).
    /// 
    /// **Funcionalidad INSERT:**
    /// - **Solo INSERT:** Crea nuevos productos (no actualiza existentes)
    /// - **productoId se genera automáticamente:** No debe enviarse en el request
    /// - **Campos de auditoría se calculan automáticamente:** No enviar fechaAlta, usuarioAlta, fechaModificacion, usuarioModificacion
    /// - **Validación de duplicados:**
    ///   - Detecta duplicados en el mismo lote (mismo nombreProducto + anioModelo + nombreLocal)
    ///   - Valida contra la base de datos antes de insertar (constraint UQ_COGM_MODELO_ANIO)
    ///   - La constraint única incluye 3 campos: COGM_NOMBREPRODUCTO, COGM_ANIOMODELO, COGM_NOMBRELOCAL
    ///   - Si ya existe un registro con la misma combinación, retorna error claro sin intentar insertar
    /// - **TODO O NADA:** Si hay errores, rollback automático completo
    /// 
    /// **Campos obligatorios:**
    /// - `nombreProducto`: Nombre del producto
    /// - `pais`: País de origen/distribución
    /// - `nombreModelo`: Nombre del modelo
    /// - `anioModelo`: Año del modelo (entre 1900 y 2100)
    /// - `modeloInteres`: Modelo de interés
    /// - `marcaNegocio`: Marca comercial
    /// 
    /// **Campos opcionales:**
    /// - `nombreLocal`: Nombre local (puede ser nulo)
    /// - `definicionVehiculo`: Definición del vehículo (puede ser nulo)
    /// 
    /// **Formato del Request (camelCase sin prefijos):**
    /// ```json
    /// {
    ///   "json": [
    ///     {
    ///       "nombreProducto": "Camaro",
    ///       "pais": "Mexico",
    ///       "nombreModelo": "CAMARO 2 DR COUPE",
    ///       "anioModelo": 2025,
    ///       "modeloInteres": "Camaro",
    ///       "marcaNegocio": "Chevrolet",
    ///       "nombreLocal": "Camaro Local",
    ///       "definicionVehiculo": "Deportivo"
    ///     }
    ///   ]
    /// }
    /// ```
    /// 
    /// ⚠️ **IMPORTANTE:**
    /// - ❌ NO enviar productoId (se genera automáticamente)
    /// - ❌ NO enviar fechaAlta, usuarioAlta, fechaModificacion, usuarioModificacion (se calculan automáticamente)
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Total de registros procesados
    /// - Registros insertados exitosamente
    /// - Registros con errores (si los hay)
    /// - Información de paginación
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="request">Objeto con el array de productos a crear</param>
    /// <returns>Resultado de la operación batch con estadísticas</returns>
    /// <response code="200">Operación batch completada exitosamente.</response>
    /// <response code="400">Error de validación o duplicados detectados.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductoBatchResultadoDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> CrearProductos([FromBody] ProductoBatchRequestDto request)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var currentUser = JwtUserHelper.GetCurrentUser(User, _logger);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validar que el array de productos no sea nulo o vacío
            if (request.Json == null || request.Json.Count == 0)
            {
                return BadRequest(new ApiResponse<ProductoBatchResultadoDto>
                {
                    Success = false,
                    Message = "El array 'json' es requerido y debe contener al menos un producto",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            _logger.LogInformation(
                "Inicio de creación de productos (batch). Usuario: {UserId}, CorrelationId: {CorrelationId}, TotalRegistros: {TotalRegistros}, Request: {@Request}",
                currentUser, correlationId, request.Json.Count, request);

            var result = await _service.ProcesarBatchInsertAsync(
                request.Json,
                currentUser,
                correlationId);

            stopwatch.Stop();
            _logger.LogInformation(
                "Productos creados exitosamente (batch). CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}, Insertados: {Insertados}, Actualizados: {Actualizados}, Errores: {Errores}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser, result.RegistrosInsertados, result.RegistrosActualizados, result.RegistrosError);

            return Ok(new ApiResponse<ProductoBatchResultadoDto>
            {
                Success = true,
                Message = result.RegistrosInsertados > 0
                    ? $"Operación batch completada exitosamente: {result.RegistrosInsertados} producto(s) insertado(s)"
                    : "No se insertaron productos",
                Data = result,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (ProductoDuplicadoException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "Duplicados detectados al crear productos. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}, Duplicados: {DuplicateCount}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser, ex.CantidadDuplicados);

            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (ProductoValidacionException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "Error de validación al crear productos. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}, Errores: {ErrorCount}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser, ex.Errors?.Count ?? 0);

            return BadRequest(new ApiResponse
            {
                Success = false,
                Message = ex.Message,
                Data = ex.Errors,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (ProductoDataAccessException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error de acceso a datos al crear productos. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error al acceder a la base de datos. Por favor, intente nuevamente.",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error inesperado al crear productos. CorrelationId: {CorrelationId}, Tiempo: {ElapsedMs}ms, Usuario: {UserId}",
                correlationId, stopwatch.ElapsedMilliseconds, currentUser);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = "Error interno del servidor. El error ha sido registrado.",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}

