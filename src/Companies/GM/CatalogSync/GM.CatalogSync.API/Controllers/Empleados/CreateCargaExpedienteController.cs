using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GM.CatalogSync.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        /// (El archivo debe estar previamente subido a Azure desde el frontend)
        /// </summary>
        /// <param name="empleadoId">ID del empleado</param>
        /// <param name="dto">Metadatos del documento ya subido</param>
        [HttpPost("empleado/{empleadoId}")]
        [ProducesResponseType(typeof(CargaExpedienteResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CrearExpediente(
            [FromRoute] int empleadoId,
            [FromBody] InsertarCargaExpedienteDto dto)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                // Obtener datos del usuario autenticado
                var empresaId = int.Parse(User.FindFirstValue("EmpresaId") ?? "0");
                var usuarioActual = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "SYSTEM";

                if (empresaId == 0)
                {
                    _logger.LogWarning(
                        "⚠️ CorrelationId {CorrelationId} - No se pudo obtener EmpresaId del token",
                        correlationId);
                    return BadRequest(new { mensaje = "No se pudo obtener la empresa del usuario" });
                }

                _logger.LogInformation(
                    "🟢 CorrelationId {CorrelationId} - POST /api/expediente/empleado/{EmpleadoId} - Usuario: {Usuario}, Empresa: {EmpresaId}",
                    correlationId,
                    empleadoId,
                    usuarioActual,
                    empresaId);

                var resultado = await _service.InsertarAsync(
                    empresaId,
                    empleadoId,
                    usuarioActual,
                    dto,
                    correlationId);

                return CreatedAtAction(
                    nameof(ObtenerExpediente),
                    new { id = resultado.IdDocumento },
                    resultado);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    "⚠️ CorrelationId {CorrelationId} - Error de validación: {Mensaje}",
                    correlationId,
                    ex.Message);
                return BadRequest(new { mensaje = ex.Message, correlationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ CorrelationId {CorrelationId} - Error al crear expediente",
                    correlationId);
                return StatusCode(500, new 
                { 
                    mensaje = "Error interno del servidor", 
                    correlationId 
                });
            }
        }

        /// <summary>
        /// Actualiza la información de un documento del expediente
        /// </summary>
        /// <param name="documentoId">ID del documento a actualizar</param>
        /// <param name="empleadoId">ID del empleado</param>
        /// <param name="dto">Datos a actualizar</param>
        [HttpPut("{documentoId}/empleado/{empleadoId}")]
        [ProducesResponseType(typeof(CargaExpedienteResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActualizarExpediente(
            [FromRoute] int documentoId,
            [FromRoute] int empleadoId,
            [FromBody] ActualizarCargaExpedienteDto dto)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                var empresaId = int.Parse(User.FindFirstValue("EmpresaId") ?? "0");
                var usuarioActual = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "SYSTEM";

                _logger.LogInformation(
                    "🟡 CorrelationId {CorrelationId} - PUT /api/expediente/{DocumentoId}/empleado/{EmpleadoId}",
                    correlationId,
                    documentoId,
                    empleadoId);

                var resultado = await _service.ActualizarAsync(
                    documentoId,
                    empresaId,
                    empleadoId,
                    usuarioActual,
                    dto,
                    correlationId);

                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(
                    "⚠️ CorrelationId {CorrelationId} - Error de validación: {Mensaje}",
                    correlationId,
                    ex.Message);
                return BadRequest(new { mensaje = ex.Message, correlationId });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    "⚠️ CorrelationId {CorrelationId} - Documento no encontrado: {Mensaje}",
                    correlationId,
                    ex.Message);
                return NotFound(new { mensaje = ex.Message, correlationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ CorrelationId {CorrelationId} - Error al actualizar expediente",
                    correlationId);
                return StatusCode(500, new 
                { 
                    mensaje = "Error interno del servidor", 
                    correlationId 
                });
            }
        }

        /// <summary>
        /// Obtiene un documento por ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerExpediente(int id)
        {
            var correlationId = Guid.NewGuid().ToString();
            
            try
            {
                // TODO: Implementar servicio para obtener el documento
                _logger.LogInformation(
                    "🔵 CorrelationId {CorrelationId} - GET /api/expediente/{Id}",
                    correlationId,
                    id);
                
                await Task.CompletedTask;
                return Ok(new { id, mensaje = "Método pendiente de implementar" });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ CorrelationId {CorrelationId} - Error al obtener expediente",
                    correlationId);
                return StatusCode(500, new 
                { 
                    mensaje = "Error interno del servidor", 
                    correlationId 
                });
            }
        }
    }
}