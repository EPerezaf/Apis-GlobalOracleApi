namespace GM.DealerSync.Domain.Entities;

/// <summary>
/// Entidad que representa un dealer para sincronización.
/// Basado en la consulta de CO_EVENTOSCARGASNAPSHOTDEALERS agrupado por URLWebhook.
/// </summary>
public class Dealer
{
    /// <summary>
    /// URL del webhook del dealer.
    /// Columna: COSD_URLWEBHOOK (agrupado)
    /// </summary>
    public string UrlWebhook { get; set; } = string.Empty;

    /// <summary>
    /// Secret key del dealer para autenticación.
    /// Columna: COSD_SECRETKEY
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Código BAC del dealer.
    /// Columna: COSD_DEALERBAC
    /// </summary>
    public string DealerBac { get; set; } = string.Empty;

    /// <summary>
    /// Nombre comercial del dealer.
    /// Columna: COSD_NOMBREDEALER
    /// </summary>
    public string NombreDealer { get; set; } = string.Empty;

    /// <summary>
    /// Estado del webhook (debe ser != 'EXITOSO' para ser procesado).
    /// Columna: COSD_ESTADOWEBHOOK
    /// </summary>
    public string EstadoWebhook { get; set; } = string.Empty;

    /// <summary>
    /// Identificador del evento de carga de proceso.
    /// Columna: COSD_COCP_EVENTOCARGAPROCESOID
    /// </summary>
    public int EventoCargaProcesoId { get; set; }
}

