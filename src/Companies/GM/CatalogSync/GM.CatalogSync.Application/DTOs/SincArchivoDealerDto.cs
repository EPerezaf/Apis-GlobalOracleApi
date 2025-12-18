using System.ComponentModel.DataAnnotations;

namespace GM.CatalogSync.Application.DTOs;

/// <summary>
/// DTO para lectura de Sincronización de Archivos por Dealer.
/// </summary>
public class SincArchivoDealerDto
{
    /// <summary>
    /// Identificador único del registro.
    /// </summary>
    public int SincArchivoDealerId { get; set; }

    /// <summary>
    /// Nombre del proceso de sincronización.
    /// </summary>
    public string Proceso { get; set; } = string.Empty;

    /// <summary>
    /// ID de la carga relacionada.
    /// </summary>
    public string IdCarga { get; set; } = string.Empty;

    /// <summary>
    /// Sistema DMS origen.
    /// </summary>
    public string DmsOrigen { get; set; } = string.Empty;

    /// <summary>
    /// Código BAC del dealer.
    /// </summary>
    public string DealerBac { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del dealer.
    /// </summary>
    public string NombreDealer { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de sincronización.
    /// </summary>
    public DateTime FechaSincronizacion { get; set; }

    /// <summary>
    /// Número de registros sincronizados.
    /// </summary>
    public int RegistrosSincronizados { get; set; }

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
/// DTO para creación de Sincronización de Archivos por Dealer.
/// </summary>
public class CrearSincArchivoDealerDto
{
    /// <summary>
    /// Nombre del proceso de sincronización (ej: ProductsCatalog).
    /// </summary>
    [Required(ErrorMessage = "El proceso es requerido")]
    [StringLength(400, ErrorMessage = "El proceso no puede exceder 400 caracteres")]
    public string Proceso { get; set; } = string.Empty;

    /// <summary>
    /// ID de la carga relacionada.
    /// </summary>
    [Required(ErrorMessage = "El ID de carga es requerido")]
    [StringLength(400, ErrorMessage = "El ID de carga no puede exceder 400 caracteres")]
    public string IdCarga { get; set; } = string.Empty;

    /// <summary>
    /// Sistema DMS origen.
    /// </summary>
    [Required(ErrorMessage = "El DMS origen es requerido")]
    [StringLength(400, ErrorMessage = "El DMS origen no puede exceder 400 caracteres")]
    public string DmsOrigen { get; set; } = string.Empty;

    /// <summary>
    /// Código BAC del dealer.
    /// </summary>
    [Required(ErrorMessage = "El código BAC del dealer es requerido")]
    [StringLength(100, ErrorMessage = "El código BAC del dealer no puede exceder 100 caracteres")]
    public string DealerBac { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del dealer.
    /// </summary>
    [Required(ErrorMessage = "El nombre del dealer es requerido")]
    [StringLength(400, ErrorMessage = "El nombre del dealer no puede exceder 400 caracteres")]
    public string NombreDealer { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de sincronización.
    /// </summary>
    [Required(ErrorMessage = "La fecha de sincronización es requerida")]
    public DateTime FechaSincronizacion { get; set; }

    /// <summary>
    /// Número de registros sincronizados.
    /// </summary>
    [Required(ErrorMessage = "El número de registros sincronizados es requerido")]
    [Range(0, int.MaxValue, ErrorMessage = "El número de registros debe ser mayor o igual a 0")]
    public int RegistrosSincronizados { get; set; }
}

/// <summary>
/// DTO para filtros de búsqueda de Sincronización de Archivos por Dealer.
/// </summary>
public class FiltrosSincArchivoDealerDto
{
    /// <summary>
    /// Filtro por nombre del proceso.
    /// </summary>
    public string? Proceso { get; set; }

    /// <summary>
    /// Filtro por ID de carga.
    /// </summary>
    public string? IdCarga { get; set; }

    /// <summary>
    /// Filtro por código BAC del dealer.
    /// </summary>
    public string? DealerBac { get; set; }
}

