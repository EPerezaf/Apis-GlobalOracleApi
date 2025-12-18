namespace GM.CatalogSync.Domain.Entities;

/// <summary>
/// Entidad de dominio para Sincronización de Archivos por Dealer.
/// Tabla: CO_SINCRONIZACIONARCHIVOSDEALERS
/// </summary>
public class SincArchivoDealer
{
    /// <summary>
    /// Identificador único del registro (COSA_SINCARCHIVODEALERID).
    /// </summary>
    public int SincArchivoDealerId { get; set; }

    /// <summary>
    /// Nombre del proceso de sincronización (COSA_PROCESO).
    /// </summary>
    public string Proceso { get; set; } = string.Empty;

    /// <summary>
    /// ID de la carga relacionada (COSA_IDCARGA).
    /// </summary>
    public string IdCarga { get; set; } = string.Empty;

    /// <summary>
    /// Sistema DMS origen (COSA_DMSORIGEN).
    /// </summary>
    public string DmsOrigen { get; set; } = string.Empty;

    /// <summary>
    /// Código BAC del dealer (COSA_DEALERBAC).
    /// </summary>
    public string DealerBac { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del dealer (COSA_NOMBREDEALER).
    /// </summary>
    public string NombreDealer { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de sincronización (COSA_FECHASINCRONIZACION).
    /// </summary>
    public DateTime FechaSincronizacion { get; set; }

    /// <summary>
    /// Número de registros sincronizados (COSA_REGISTROSSINCRONIZADOS).
    /// </summary>
    public int RegistrosSincronizados { get; set; }

    // Campos de auditoría
    /// <summary>
    /// Fecha de alta del registro (FECHAALTA).
    /// </summary>
    public DateTime FechaAlta { get; set; }

    /// <summary>
    /// Usuario que realizó el alta (USUARIOALTA).
    /// </summary>
    public string UsuarioAlta { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de última modificación (FECHAMODIFICACION).
    /// </summary>
    public DateTime? FechaModificacion { get; set; }

    /// <summary>
    /// Usuario que realizó la última modificación (USUARIOMODIFICACION).
    /// </summary>
    public string? UsuarioModificacion { get; set; }
}

