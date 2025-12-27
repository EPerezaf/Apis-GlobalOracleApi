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
    /// Token de confirmación generado automáticamente.
    /// Hash SHA256 de: idCarga + dealerBac + fechaSincronizacion + registrosSincronizados
    /// </summary>
    public string TokenConfirmacion { get; set; } = string.Empty;

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
    /// ID de la carga de archivo de sincronización relacionada (FK).
    /// Debe existir en CO_CARGAARCHIVOSINCRONIZACION y estar activo (COCA_ACTUAL=1).
    /// </summary>
    [Required(ErrorMessage = "El ID de carga de archivo de sincronización es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID de carga debe ser mayor a 0")]
    public int CargaArchivoSincronizacionId { get; set; }

    /// <summary>
    /// Código BAC del dealer (DEALERID en CO_DISTRIBUIDORES).
    /// </summary>
    [Required(ErrorMessage = "El código BAC del dealer es requerido")]
    [StringLength(100, ErrorMessage = "El código BAC del dealer no puede exceder 100 caracteres")]
    public string DealerBac { get; set; } = string.Empty;

    // NOTA: Los siguientes campos se calculan automáticamente y NO deben enviarse en el request:
    // - proceso: Se obtiene de CO_CARGAARCHIVOSINCRONIZACION.COCA_PROCESO mediante JOIN
    // - registrosSincronizados: Se obtiene de CO_CARGAARCHIVOSINCRONIZACION.COCA_REGISTROS
    // - dmsOrigen: Se consulta de CO_DISTRIBUIDORES usando dealerBac (DEALERID)
    // - nombreDealer: Se consulta de CO_DISTRIBUIDORES usando dealerBac (DEALERID)
    // - fechaSincronizacion: Se calcula automáticamente con hora de México
    // - sincArchivoDealerId: Se genera automáticamente por secuencia
    // - fechaAlta: SYSDATE
    // - usuarioAlta: Se toma del JWT token
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

