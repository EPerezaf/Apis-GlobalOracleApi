namespace GM.DealersSincronizacion.Application.DTOs;

/// <summary>
/// DTO para lectura de Evento de Carga de Proceso (sin campos de dealers para privacidad).
/// </summary>
public class EventoCargaProcesoDto
{
    /// <summary>
    /// Identificador único del registro de evento de carga.
    /// </summary>
    public int EventoCargaProcesoId { get; set; }

    /// <summary>
    /// Nombre del proceso de sincronización.
    /// </summary>
    public string Proceso { get; set; } = string.Empty;

    /// <summary>
    /// Nombre completo del archivo cargado.
    /// </summary>
    public string NombreArchivo { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora de la carga.
    /// </summary>
    public DateTime FechaCarga { get; set; }

    /// <summary>
    /// Identificador único de la carga.
    /// </summary>
    public string IdCarga { get; set; } = string.Empty;

    /// <summary>
    /// Número de registros procesados.
    /// </summary>
    public int Registros { get; set; }

    /// <summary>
    /// Indica si es la carga actual.
    /// </summary>
    public bool Actual { get; set; }

    /// <summary>
    /// Nombre de la tabla relacionada.
    /// </summary>
    public string? TablaRelacion { get; set; }

    /// <summary>
    /// Componente relacionado.
    /// </summary>
    public string? ComponenteRelacionado { get; set; }

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

    // NOTA: Los campos DealersTotales, DealersSincronizados y PorcDealersSinc NO se exponen a los dealers por privacidad
}

