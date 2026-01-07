namespace GM.DealerSync.Domain.Entities;

/// <summary>
/// Entidad que representa un registro de snapshot de dealers en un evento de carga de proceso.
/// Tabla: CO_EVENTOSCARGASNAPSHOTDEALERS
/// </summary>
public class EventoCargaSnapshotDealer
{
    /// <summary>
    /// Identificador único del registro (PK).
    /// Columna: COSD_EVENTOCARGASNAPDEALERID
    /// </summary>
    public int EventoCargaSnapshotDealerId { get; set; }

    /// <summary>
    /// Identificador del evento de carga de proceso (FK).
    /// Columna: COSD_COCP_EVENTOCARGAPROCESOID
    /// </summary>
    public int EventoCargaProcesoId { get; set; }

    /// <summary>
    /// Código BAC del dealer (FK).
    /// Columna: COSD_DEALERBAC
    /// </summary>
    public string DealerBac { get; set; } = string.Empty;

    /// <summary>
    /// Nombre comercial del dealer.
    /// Columna: COSD_NOMBREDEALER
    /// </summary>
    public string NombreDealer { get; set; } = string.Empty;

    /// <summary>
    /// Sistema DMS utilizado.
    /// Columna: COSD_DMS
    /// </summary>
    public string Dms { get; set; } = string.Empty;

    /// <summary>
    /// URL del webhook del distribuidor.
    /// Columna: COSD_URLWEBHOOK
    /// </summary>
    public string? UrlWebhook { get; set; }

    /// <summary>
    /// Secret key del webhook del distribuidor.
    /// Columna: COSD_SECRETKEY
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Estado del webhook.
    /// Columna: COSD_ESTADOWEBHOOK
    /// </summary>
    public string? EstadoWebhook { get; set; }

    /// <summary>
    /// Intentos de webhook.
    /// Columna: COSD_INTENTOSWEBHOOK
    /// </summary>
    public int? IntentosWebhook { get; set; }

    /// <summary>
    /// Último intento de webhook.
    /// Columna: COSD_ULTIMOINTENTOWEBHOOK
    /// </summary>
    public DateTime? UltimoIntentoWebhook { get; set; }

    /// <summary>
    /// Último error de webhook.
    /// Columna: COSD_ULTIMOERRORWEBHOOK
    /// </summary>
    public string? UltimoErrorWebhook { get; set; }
}

