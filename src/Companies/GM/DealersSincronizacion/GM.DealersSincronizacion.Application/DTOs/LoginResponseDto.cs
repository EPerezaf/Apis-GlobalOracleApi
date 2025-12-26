namespace GM.DealersSincronizacion.Application.DTOs;

/// <summary>
/// DTO para respuesta de login de dealer.
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// Token JWT generado para el dealer.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Código BAC del dealer autenticado.
    /// </summary>
    public string DealerBac { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora de expiración del token.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}



