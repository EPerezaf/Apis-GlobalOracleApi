using System.ComponentModel.DataAnnotations;

namespace GM.DealersSincronizacion.Application.DTOs;

/// <summary>
/// DTO para solicitud de login de dealer.
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// Código BAC del dealer (usuario).
    /// </summary>
    [Required(ErrorMessage = "El código BAC del dealer es requerido")]
    public string DealerBac { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña del dealer.
    /// </summary>
    [Required(ErrorMessage = "La contraseña es requerida")]
    public string Password { get; set; } = string.Empty;
}





