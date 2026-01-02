using System.ComponentModel.DataAnnotations;

namespace GM.CatalogSync.Application.DTOs;

/// <summary>
/// DTO para lectura de Sincronización de Carga de Proceso por Dealer.
/// </summary>
public class SincCargaProcesoDealerDto
{
    /// <summary>
    /// Identificador único del registro.
    /// </summary>
    public int SincCargaProcesoDealerId { get; set; }

    /// <summary>
    /// Nombre del proceso de sincronización.
    /// </summary>
    public string Proceso { get; set; } = string.Empty;

    /// <summary>
    /// ID del evento de carga de proceso relacionado (FK).
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
    /// Hash SHA256 de: idCarga + dealerBac + proceso + fechaSincronizacion + registrosSincronizados
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
/// DTO para creación de Sincronización de Carga de Proceso por Dealer.
/// </summary>
public class CrearSincCargaProcesoDealerDto
{
    /// <summary>
    /// ID del evento de carga de proceso relacionado (FK).
    /// Debe existir en CO_EVENTOSCARGAPROCESO y estar activo (COCP_ACTUAL=1).
    /// </summary>
    [Required(ErrorMessage = "El ID de evento de carga de proceso es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID de evento de carga debe ser mayor a 0")]
    public int EventoCargaProcesoId { get; set; }

    // NOTA: Los siguientes campos se calculan automáticamente y NO deben enviarse en el request:
    // - proceso: Se obtiene de CO_EVENTOSCARGAPROCESO.COCP_PROCESO mediante JOIN
    // - registrosSincronizados: Se obtiene de CO_EVENTOSCARGAPROCESO.COCP_REGISTROS
    // - dmsOrigen: Se consulta de CO_DISTRIBUIDORES usando dealerBac del JWT (ej: "GDMS")
    // - dealerBac: Se toma del JWT token (ej: "290487")
    // - nombreDealer: Se consulta de CO_DISTRIBUIDORES usando dealerBac del JWT (ej: "CHEVROLET CAR ONE RUIZ CORTINES")
    // - fechaSincronizacion: Se calcula automáticamente con hora de México
    // - sincCargaProcesoDealerId: Se genera automáticamente por secuencia
    // - fechaAlta: SYSDATE
    // - usuarioAlta: Se toma del JWT token
}

/// <summary>
/// DTO para filtros de búsqueda de Sincronización de Carga de Proceso por Dealer.
/// </summary>
public class FiltrosSincCargaProcesoDealerDto
{
    /// <summary>
    /// Filtro por nombre del proceso.
    /// </summary>
    public string? Proceso { get; set; }

    /// <summary>
    /// Filtro por ID de evento de carga de proceso.
    /// </summary>
    public int? EventoCargaProcesoId { get; set; }

    /// <summary>
    /// Filtro por código BAC del dealer.
    /// </summary>
    public string? DealerBac { get; set; }
}

