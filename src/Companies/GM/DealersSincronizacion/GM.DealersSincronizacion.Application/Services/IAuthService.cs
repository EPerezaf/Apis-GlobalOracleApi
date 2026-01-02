using GM.DealersSincronizacion.Application.DTOs;

namespace GM.DealersSincronizacion.Application.Services;

/// <summary>
/// Interfaz del servicio de autenticación para dealers.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Valida las credenciales del dealer y genera un token JWT.
    /// </summary>
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
}





