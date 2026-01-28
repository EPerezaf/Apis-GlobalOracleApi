using System.Diagnostics;
using System.Security.Claims;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;
using Shared.Exceptions;

namespace GM.CatalogSync.API.Controllers.Empleado
{
    [ApiController]
    [Route("api/v1/common/erp/empleados/{empleadoId}/expediente")]
    [Authorize]
    [Tags("ERP")]
    public class CreateCargaExpedienteController : ControllerBase
    {
        private readonly ICargaExpedienteService _service;
        private readonly ILogger<CreateCargaExpedienteController> _logger;

        public CreateCargaExpedienteController(
            ICargaExpedienteService service,
            ILogger<CreateCargaExpedienteController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Registra un nuevo documento en el expediente del empleado
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CrearExpediente(
            [FromRoute] int empleadoId,
            [FromBody] CrearCargaExpedienteDto dto)
        {
            var correlationId = HttpContext.TraceIdentifier;
            var currentUser = JwtUserHelper.GetCurrentUser(User, _logger);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validar ID empleado
                if (empleadoId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "El ID del empleado no es válido",
                        Timestamp = DateTimeHelper.GetMexicoTimeString()
                    });
                }

                // Obtener empresaId del token JWT
                var empresaId = JwtUserHelper.GetEmpresaId(User, _logger);
                if (!empresaId.HasValue)
                {
                    _logger.LogWarning(
                        "[{CorrelationId}] ⚠️ EmpresaId no encontrado en el token JWT",
                        correlationId);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "No se pudo obtener la empresa del usuario desde el token JWT.",
                        Timestamp = DateTimeHelper.GetMexicoTimeString()
                    });
                }

                // Validar DTO
                if (dto == null)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Los datos del expediente son requeridos",
                        Timestamp = DateTimeHelper.GetMexicoTimeString()
                    });
                }

                _logger.LogInformation(
                    "[{CorrelationId}] 📡 Iniciando POST crear expediente - EmpleadoId: {EmpleadoId}, EmpresaId: {EmpresaId}, Usuario: {Usuario}",
                    correlationId, empleadoId, empresaId, currentUser);

                var empresaIdValue = empresaId.Value;

                var resultadoDto = await _service.CrearExpedienteAsync(
                    empresaIdValue,
                    empleadoId,
                    currentUser,
                    dto,
                    correlationId);

                int idFinal = resultadoDto.IdDocumento;
                stopwatch.Stop();

                _logger.LogInformation(
                    "[{CorrelationId}] ✅ Expediente creado exitosamente - DocumentoId: {DocumentoId}, Tiempo: {ElapsedMilliseconds}ms",
                    correlationId, idFinal, stopwatch.ElapsedMilliseconds);

                return CreatedAtAction(
            nameof(ObtenerDocumento),
            new { empleadoId, documentoId = idFinal },
            new ApiResponse<int>
            {
                Success = true,
                Message = "Documento registrado exitosamente en el expediente",
                Data = idFinal,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
            }
            catch (ArgumentNullException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ Error de validación (null): {Mensaje}",
                    correlationId, ex.Message);

                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
            catch (ArgumentException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ Error de validación: {Mensaje}",
                    correlationId, ex.Message);

                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
            catch (BusinessValidationException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[{CorrelationId}] ⚠️ Error de validación de negocio: {Mensaje}",
                    correlationId, ex.Message);

                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = ex.Errors?.Select(err => new ErrorDetail
                    {
                        Field = err.Field,
                        Message = err.Message,
                        Code = "VALIDATION_ERROR"
                    }).ToList(),
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(
                    ex,
                    "[{CorrelationId}] ❌ Error crítico al crear expediente",
                    correlationId);

                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error interno del servidor al procesar el expediente.",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }

        /// <summary>
        /// Actualiza la información de un documento del expediente
        /// </summary>
        [HttpPut("{documentoId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ActualizarExpediente(
    [FromRoute] int empleadoId,
    [FromRoute] int documentoId,
    [FromBody] ActualizarCargaExpedienteDto dto)
        {
            var correlationId = HttpContext.TraceIdentifier;
            var currentUser = JwtUserHelper.GetCurrentUser(User, _logger);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validaciones iniciales
                if (empleadoId <= 0 || documentoId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Los IDs proporcionados no son válidos",
                        Timestamp = DateTimeHelper.GetMexicoTimeString()
                    });
                }

                if (dto == null || documentoId != dto.IdDocumento)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "Datos de documento inválidos o inconsistencia en ID",
                        Timestamp = DateTimeHelper.GetMexicoTimeString()
                    });
                }

                // Obtener empresaId del token
                var empresaId = int.Parse(User.FindFirstValue("EmpresaId") ?? "0");
                if (empresaId <= 0)
                {
                    _logger.LogWarning("[{CorrelationId}] ⚠️ EmpresaId no encontrado en token para Usuario: {User}", correlationId, currentUser);
                    return Unauthorized(new ApiResponse
                    {
                        Success = false,
                        Message = "No se pudo obtener la empresa del usuario.",
                        Timestamp = DateTimeHelper.GetMexicoTimeString()
                    });
                }

                _logger.LogInformation(
                    "[{CorrelationId}] 📡 Iniciando actualización de expediente - DocumentoId: {DocumentoId}, EmpleadoId: {EmpleadoId}, Usuario: {Usuario}",
                    correlationId, documentoId, empleadoId, currentUser);

                // Ejecución del servicio
                await _service.ActualizarExpedienteAsync(
                    documentoId,
                    empresaId,
                    empleadoId,
                    currentUser,
                    dto,
                    correlationId);

                stopwatch.Stop();
                _logger.LogInformation(
                    "[{CorrelationId}] ✅ Expediente actualizado exitosamente - DocumentoId: {DocumentoId}, Tiempo: {ElapsedMs}ms, Usuario: {Usuario}",
                    correlationId, documentoId, stopwatch.ElapsedMilliseconds, currentUser);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "[{CorrelationId}] ⚠️ Error de validación al actualizar: {Mensaje}, Tiempo: {ElapsedMs}ms",
                    correlationId, ex.Message, stopwatch.ElapsedMilliseconds);

                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
            catch (InvalidOperationException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "[{CorrelationId}] ⚠️ Documento no encontrado: {DocumentoId}, Tiempo: {ElapsedMs}ms",
                    correlationId, documentoId, stopwatch.ElapsedMilliseconds);

                return NotFound(new ApiResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
            catch (DataAccessException ex) 
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[{CorrelationId}] ❌ Error de acceso a datos en actualización, Tiempo: {ElapsedMs}ms",
                    correlationId, stopwatch.ElapsedMilliseconds);

                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error al procesar la actualización en la base de datos.",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[{CorrelationId}] ❌ Error inesperado al actualizar expediente, Tiempo: {ElapsedMs}ms",
                    correlationId, stopwatch.ElapsedMilliseconds);

                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error interno del servidor.",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }

        /// <summary>
        /// Obtiene un documento por ID
        /// </summary>
        [HttpGet("{documentoId}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ObtenerDocumento(
            [FromRoute] int empleadoId,
            [FromRoute] int documentoId)
        {
            var correlationId = HttpContext.TraceIdentifier;

            try
            {
                // Validaciones
                if (empleadoId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "El ID del empleado no es válido",
                        Timestamp = DateTimeHelper.GetMexicoTimeString()
                    });
                }

                if (documentoId <= 0)
                {
                    return BadRequest(new ApiResponse
                    {
                        Success = false,
                        Message = "El ID del documento no es válido",
                        Timestamp = DateTimeHelper.GetMexicoTimeString()
                    });
                }

                _logger.LogInformation(
                    "[{CorrelationId}] 📡 GET obtener expediente - DocumentoId: {DocumentoId}, EmpleadoId: {EmpleadoId}",
                    correlationId,
                    documentoId,
                    empleadoId);

                var resultado = await _service.ObtenerPorIdAsync(documentoId, correlationId);

                if (resultado == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Success = false,
                        Message = $"No se encontró el expediente con ID {documentoId}",
                        Timestamp = DateTimeHelper.GetMexicoTimeString()
                    });
                }

                _logger.LogInformation(
                    "[{CorrelationId}] ✅ Expediente obtenido exitosamente - DocumentoId: {DocumentoId}",
                    correlationId,
                    documentoId);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[{CorrelationId}] ❌ Error al obtener expediente",
                    correlationId);
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error interno del servidor",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }
        }
    }
}