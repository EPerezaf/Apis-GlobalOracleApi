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
    /// Nombre del dealer (obtenido de CO_DISTRIBUIDORES.CODI_NOMBRE).
    /// </summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Razón social del dealer (obtenido de CO_DISTRIBUIDORES.CODI_RAZONSOCIAL).
    /// </summary>
    public string RazonSocial { get; set; } = string.Empty;

    /// <summary>
    /// Sistema DMS del dealer (obtenido de CO_DISTRIBUIDORES.CODI_DMS, por defecto "GDMS" si está vacío).
    /// </summary>
    public string Dms { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora de expiración del token.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}



