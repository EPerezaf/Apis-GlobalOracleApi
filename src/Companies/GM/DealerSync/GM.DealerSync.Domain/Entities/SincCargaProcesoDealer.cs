namespace GM.DealerSync.Domain.Entities;

/// <summary>
/// Entidad de dominio para Sincronización de Carga de Proceso por Dealer.
/// Tabla: CO_SINCRONIZACIONCARGAPROCESODEALER
/// </summary>
public class SincCargaProcesoDealer
{
    /// <summary>
    /// Identificador único del registro (COSC_SINCARGAPROCESODEALERID).
    /// </summary>
    public int SincCargaProcesoDealerId { get; set; }

    /// <summary>
    /// ID del evento de carga de proceso relacionado (COSC_COCP_EVENTOCARGAPROCESOID) - FK a CO_EVENTOSCARGAPROCESO.
    /// </summary>
    public int EventoCargaProcesoId { get; set; }

    /// <summary>
    /// Sistema DMS origen (COSC_DMSORIGEN).
    /// </summary>
    public string DmsOrigen { get; set; } = string.Empty;

    /// <summary>
    /// Código BAC del dealer (COSC_DEALERBAC).
    /// </summary>
    public string DealerBac { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del dealer (COSC_NOMBREDEALER).
    /// </summary>
    public string NombreDealer { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de sincronización (COSC_FECHASINCRONIZACION).
    /// </summary>
    public DateTime FechaSincronizacion { get; set; }

    /// <summary>
    /// Número de registros sincronizados (COSC_REGISTROSSINCRONIZADOS).
    /// </summary>
    public int RegistrosSincronizados { get; set; }

    /// <summary>
    /// Token de confirmación generado automáticamente (COSC_TOKENCONFIRMACION).
    /// </summary>
    public string TokenConfirmacion { get; set; } = string.Empty;

    // Campos de auditoría
    /// <summary>
    /// Fecha de alta del registro (COSC_FECHAALTA).
    /// </summary>
    public DateTime FechaAlta { get; set; }

    /// <summary>
    /// Usuario que realizó el alta (COSC_USUARIOALTA).
    /// </summary>
    public string UsuarioAlta { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de última modificación (COSC_FECHAMODIFICACION).
    /// </summary>
    public DateTime? FechaModificacion { get; set; }

    /// <summary>
    /// Usuario que realizó la última modificación (COSC_USUARIOMODIFICACION).
    /// </summary>
    public string? UsuarioModificacion { get; set; }
}

