using GM.DealersSincronizacion.Application.DTOs;
using GM.DealersSincronizacion.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Exceptions;
using Shared.Security;
using System.Diagnostics;

namespace GM.DealersSincronizacion.API.Controllers;

/// <summary>
/// Controller para autenticación de dealers.
/// </summary>
[ApiController]
[Route("api/v1/gm/dealer-sinc-productos/login-sincronizacion")]
public class LoginSincronizacionController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<LoginSincronizacionController> _logger;

    public LoginSincronizacionController(
        IAuthService authService,
        ILogger<LoginSincronizacionController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Autentica un dealer y genera un token JWT.
    /// </summary>
    /// <remarks>
    /// Este endpoint valida las credenciales del dealer (dealerBac y password) y genera un token JWT
    /// que incluye el dealerBac como claim. El token es válido por 24 horas y debe ser usado en
    /// las peticiones subsiguientes mediante el header Authorization: Bearer {token}.
    /// 
    /// **Funcionalidad:**
    /// - Valida las credenciales del dealer contra un diccionario de dealers autorizados
    /// - Genera un token JWT con el dealerBac como claim (DEALERBAC)
    /// - El token incluye información del usuario (USUARIO, nameid) para identificación
    /// - El token expira en 24 horas desde su emisión
    /// 
    /// **Validaciones:**
    /// - El campo `dealerBac` es requerido y no puede estar vacío
    /// - El campo `password` es requerido y no puede estar vacío
    /// - Las credenciales deben coincidir con un dealer autorizado en el sistema
    /// - Si las credenciales son incorrectas, retorna error 400 Bad Request
    /// 
    /// **Campos obligatorios en el Request Body:**
    /// - `dealerBac`: Código BAC del dealer (ej: "319333")
    /// - `password`: Contraseña del dealer (formato: "{dealerBac}#2025;", ej: "319333#2025;")
    /// 
    /// **Formato del Request:**
    /// ```json
    /// {
    ///   "dealerBac": "319333",
    ///   "password": "319333#2025;"
    /// }
    /// ```
    /// 
    /// **Campos en la respuesta:**
    /// - `token`: Token JWT que debe usarse en las peticiones subsiguientes
    /// - `dealerBac`: Código BAC del dealer autenticado
    /// - `expiresIn`: Tiempo de expiración del token en segundos (86400 = 24 horas)
    /// 
    /// ⚠️ **IMPORTANTE:**
    /// - ✅ El token debe incluirse en todas las peticiones subsiguientes mediante el header `Authorization: Bearer {token}`
    /// - ✅ El token expira en 24 horas, después de ese tiempo será necesario autenticarse nuevamente
    /// - ✅ El dealerBac del token se usa automáticamente en otros endpoints para filtros y validaciones
    /// - ❌ NO compartir el token con terceros
    /// - ❌ NO almacenar el token en lugares inseguros
    /// 
    /// **Respuesta exitosa incluye:**
    /// - Token JWT válido por 24 horas
    /// - Información del dealer autenticado
    /// - Timestamp de la operación
    /// </remarks>
    /// <param name="request">Credenciales del dealer (dealerBac y password)</param>
    /// <returns>Token JWT y información del dealer autenticado</returns>
    /// <response code="200">Autenticación exitosa. Retorna token JWT y datos del dealer.</response>
    /// <response code="400">Error de validación. Credenciales incorrectas o campos faltantes.</response>
    /// <response code="500">Error interno del servidor.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "🔐 [CONTROLLER] Intento de login. DealerBac: {DealerBac}, CorrelationId: {CorrelationId}",
            request.DealerBac, correlationId);

        try
        {
            var response = await _authService.LoginAsync(request);

            stopwatch.Stop();
            _logger.LogInformation(
                "✅ [CONTROLLER] Login exitoso. DealerBac: {DealerBac}, Tiempo: {ElapsedMs}ms",
                request.DealerBac, stopwatch.ElapsedMilliseconds);

            return Ok(new ApiResponse<LoginResponseDto>
            {
                Success = true,
                Message = "Login exitoso",
                Data = response,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (BusinessValidationException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex,
                "⚠️ [CONTROLLER] Error de validación en login. DealerBac: {DealerBac}, Tiempo: {ElapsedMs}ms",
                request.DealerBac, stopwatch.ElapsedMilliseconds);

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
                "❌ [CONTROLLER] Error inesperado en login. DealerBac: {DealerBac}, Tiempo: {ElapsedMs}ms",
                request.DealerBac, stopwatch.ElapsedMilliseconds);

            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Error interno del servidor",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}

