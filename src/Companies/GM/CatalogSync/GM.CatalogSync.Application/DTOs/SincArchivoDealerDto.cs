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
    /// ID de la carga de archivo de sincronización relacionada (FK).
    /// </summary>
    public int CargaArchivoSincronizacionId { get; set; }

    /// <summary>
    /// ID de la carga (desde CO_CARGAARCHIVOSINCRONIZACION).
    /// </summary>
    public string IdCarga { get; set; } = string.Empty;

    /// <summary>
    /// Proceso de la carga (desde CO_CARGAARCHIVOSINCRONIZACION).
    /// </summary>
    public string ProcesoCarga { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de carga (desde CO_CARGAARCHIVOSINCRONIZACION).
    /// </summary>
    public DateTime FechaCarga { get; set; }

    /// <summary>
    /// Tiempo de sincronización en horas (diferencia entre FechaSincronizacion y FechaCarga).
    /// </summary>
    public decimal TiempoSincronizacionHoras { get; set; }

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
    /// ID de la carga de archivo de sincronización relacionada (FK).
    /// </summary>
    [Required(ErrorMessage = "El ID de carga de archivo de sincronización es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID de carga debe ser mayor a 0")]
    public int CargaArchivoSincronizacionId { get; set; }

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

    // NOTA: FechaSincronizacion se calcula automáticamente en el servicio (no se envía en el request)

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
    /// Filtro por ID de carga de archivo de sincronización.
    /// </summary>
    public int? CargaArchivoSincronizacionId { get; set; }

    /// <summary>
    /// Filtro por código BAC del dealer.
    /// </summary>
    public string? DealerBac { get; set; }
}

