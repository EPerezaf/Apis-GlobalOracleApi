namespace GM.CatalogSync.Domain.Entities;

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
    /// Razón social legal del dealer.
    /// Columna: COSD_RAZONSOCIALDEALER
    /// </summary>
    public string RazonSocialDealer { get; set; } = string.Empty;

    /// <summary>
    /// Sistema DMS utilizado.
    /// Columna: COSD_DMS
    /// </summary>
    public string Dms { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de registro del snapshot.
    /// Columna: COSD_FECHAREGISTRO
    /// </summary>
    public DateTime FechaRegistro { get; set; }

    // ========================================
    // CAMPOS DE AUDITORÍA
    // ========================================

    /// <summary>
    /// Fecha de creación del registro.
    /// Columna: COSD_FECHAALTA
    /// </summary>
    public DateTime FechaAlta { get; set; }

    /// <summary>
    /// Usuario que creó el registro.
    /// Columna: COSD_USUARIOALTA
    /// </summary>
    public string UsuarioAlta { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de última modificación del registro.
    /// Columna: COSD_FECHAMODIFICACION
    /// </summary>
    public DateTime? FechaModificacion { get; set; }

    /// <summary>
    /// Usuario que realizó la última modificación.
    /// Columna: COSD_USUARIOMODIFICACION
    /// </summary>
    public string? UsuarioModificacion { get; set; }

    // ========================================
    // CAMPOS DE WEBHOOK
    // ========================================

    /// <summary>
    /// URL del webhook del distribuidor (obtenido de CO_DISTRIBUIDORES.CODI_URLWEBHOOK).
    /// Columna: COSD_URLWEBHOOK
    /// </summary>
    public string? UrlWebhook { get; set; }

    /// <summary>
    /// Secret key del webhook del distribuidor (obtenido de CO_DISTRIBUIDORES.CODI_SECRETKEY).
    /// Columna: COSD_SECRETKEY
    /// </summary>
    public string? SecretKey { get; set; }
}

