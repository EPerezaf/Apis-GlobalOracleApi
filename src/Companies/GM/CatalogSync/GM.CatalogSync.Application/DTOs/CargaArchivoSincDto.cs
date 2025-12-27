using System.ComponentModel.DataAnnotations;

namespace GM.CatalogSync.Application.DTOs;

/// <summary>
/// DTO para lectura de Carga de Archivo de Sincronización.
/// </summary>
public class CargaArchivoSincDto
{
    /// <summary>
    /// Identificador único del registro de carga.
    /// </summary>
    public int CargaArchivoSincId { get; set; }

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
    /// Número total de dealers a sincronizar.
    /// </summary>
    public int DealersTotales { get; set; }

    /// <summary>
    /// Número de dealers sincronizados.
    /// </summary>
    public int? DealersSincronizados { get; set; }

    /// <summary>
    /// Porcentaje de dealers sincronizados.
    /// </summary>
    public decimal? PorcDealersSinc { get; set; }

    /// <summary>
    /// Nombre de la tabla relacionada.
    /// </summary>
    public string? TablaRelacion { get; set; }

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
/// DTO para creación de Carga de Archivo de Sincronización.
/// </summary>
public class CrearCargaArchivoSincDto
{
    /// <summary>
    /// Nombre del proceso de sincronización (ej: ProductsCatalog, InventorySync).
    /// </summary>
    [Required(ErrorMessage = "El proceso es requerido")]
    [StringLength(400, ErrorMessage = "El proceso no puede exceder 400 caracteres")]
    public string Proceso { get; set; } = string.Empty;

    /// <summary>
    /// Nombre completo del archivo cargado.
    /// </summary>
    [Required(ErrorMessage = "El nombre del archivo es requerido")]
    [StringLength(400, ErrorMessage = "El nombre del archivo no puede exceder 400 caracteres")]
    public string NombreArchivo { get; set; } = string.Empty;

    // NOTA: FechaCarga se calcula automáticamente en el servicio (no se envía en el request)

    /// <summary>
    /// Identificador único de la carga (debe ser único en la tabla).
    /// Formato sugerido: proceso_fecha_hora
    /// </summary>
    [Required(ErrorMessage = "El ID de carga es requerido")]
    [StringLength(400, ErrorMessage = "El ID de carga no puede exceder 400 caracteres")]
    public string IdCarga { get; set; } = string.Empty;

    /// <summary>
    /// Número de registros procesados en la carga.
    /// </summary>
    [Required(ErrorMessage = "El número de registros es requerido")]
    [Range(0, int.MaxValue, ErrorMessage = "El número de registros debe ser mayor o igual a 0")]
    public int Registros { get; set; }

    /// <summary>
    /// Número total de dealers a sincronizar.
    /// </summary>
    [Required(ErrorMessage = "El número de dealers totales es requerido")]
    [Range(0, int.MaxValue, ErrorMessage = "El número de dealers totales debe ser mayor o igual a 0")]
    public int DealersTotales { get; set; }

    /// <summary>
    /// Nombre de la tabla relacionada (opcional).
    /// </summary>
    [StringLength(400, ErrorMessage = "El nombre de la tabla relacionada no puede exceder 400 caracteres")]
    public string? TablaRelacion { get; set; }
}

/// <summary>
/// DTO para filtros de búsqueda de Carga de Archivo de Sincronización.
/// </summary>
public class FiltrosCargaArchivoSincDto
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
    /// Filtro por estado actual (true=actual, false=no actual).
    /// </summary>
    public bool? Actual { get; set; }
}

