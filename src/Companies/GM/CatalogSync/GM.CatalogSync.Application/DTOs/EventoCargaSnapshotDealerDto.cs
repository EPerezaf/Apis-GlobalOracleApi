using System.ComponentModel.DataAnnotations;

namespace GM.CatalogSync.Application.DTOs;

/// <summary>
/// DTO para lectura de Evento de Carga Snapshot de Dealers.
/// </summary>
public class EventoCargaSnapshotDealerDto
{
    /// <summary>
    /// Identificador único del registro.
    /// </summary>
    public int EventoCargaSnapshotDealerId { get; set; }

    /// <summary>
    /// Identificador del evento de carga de proceso (FK).
    /// </summary>
    public int EventoCargaProcesoId { get; set; }

    /// <summary>
    /// ID de la carga (desde CO_EVENTOSCARGAPROCESO).
    /// </summary>
    public string IdCarga { get; set; } = string.Empty;

    /// <summary>
    /// Proceso de la carga (desde CO_EVENTOSCARGAPROCESO).
    /// </summary>
    public string ProcesoCarga { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de carga (desde CO_EVENTOSCARGAPROCESO).
    /// </summary>
    public DateTime FechaCarga { get; set; }

    /// <summary>
    /// Fecha de sincronización (desde CO_SINCRONIZACIONCARGAPROCESODEALER, puede ser null si no existe registro).
    /// </summary>
    public DateTime? FechaSincronizacion { get; set; }

    /// <summary>
    /// Token de confirmación (desde CO_SINCRONIZACIONCARGAPROCESODEALER, puede ser null si no existe registro).
    /// </summary>
    public string? TokenConfirmacion { get; set; }

    /// <summary>
    /// Tiempo de sincronización en horas (diferencia entre FechaSincronizacion y FechaCarga).
    /// Si FechaSincronizacion es null, este valor será null.
    /// </summary>
    public decimal? TiempoSincronizacionHoras { get; set; }

    /// <summary>
    /// Código BAC del dealer (FK).
    /// </summary>
    public string DealerBac { get; set; } = string.Empty;

    /// <summary>
    /// Nombre comercial del dealer.
    /// </summary>
    public string NombreDealer { get; set; } = string.Empty;

    /// <summary>
    /// Razón social legal del dealer.
    /// </summary>
    public string RazonSocialDealer { get; set; } = string.Empty;

    /// <summary>
    /// Sistema DMS utilizado.
    /// </summary>
    public string Dms { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de registro del snapshot.
    /// </summary>
    public DateTime FechaRegistro { get; set; }

    /// <summary>
    /// Fecha de alta del registro.
    /// </summary>
    public DateTime FechaAlta { get; set; }

    /// <summary>
    /// Usuario que realizó el alta.
    /// </summary>
    public string UsuarioAlta { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de última modificación.
    /// </summary>
    public DateTime? FechaModificacion { get; set; }

    /// <summary>
    /// Usuario que realizó la última modificación.
    /// </summary>
    public string? UsuarioModificacion { get; set; }
}

/// <summary>
/// DTO para carga batch de Eventos de Carga Snapshot de Dealers.
/// Los distribuidores se generan automáticamente desde CO_DISTRIBUIDORES basándose en el empresaId del JWT.
/// </summary>
public class CrearEventoCargaSnapshotDealerBatchDto
{
    /// <summary>
    /// Identificador del evento de carga de proceso (FK).
    /// Los distribuidores se obtendrán automáticamente desde CO_DISTRIBUIDORES filtrados por empresaId del JWT.
    /// </summary>
    [Required(ErrorMessage = "El ID de evento de carga de proceso es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID de evento de carga debe ser mayor a 0")]
    public int EventoCargaProcesoId { get; set; }

    // NOTA: Los siguientes campos se obtienen automáticamente desde CO_DISTRIBUIDORES:
    // - dealerBac: DEALERID
    // - nombreDealer: CODI_NOMBRE
    // - razonSocialDealer: CODI_RAZONSOCIAL
    // - dms: CODI_DMS (con default "GDMS" si está vacío)
    // - fechaRegistro: Se calcula automáticamente con hora de México
}

