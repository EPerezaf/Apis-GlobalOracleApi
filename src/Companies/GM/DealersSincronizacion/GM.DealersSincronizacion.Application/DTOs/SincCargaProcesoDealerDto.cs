using System.ComponentModel.DataAnnotations;

namespace GM.DealersSincronizacion.Application.DTOs;

/// <summary>
/// DTO para creación de sincronización de carga de proceso por dealer.
/// </summary>
public class CrearSincCargaProcesoDealerDto
{
    /// <summary>
    /// ID del evento de carga de proceso relacionado (FK).
    /// Debe existir en CO_EVENTOSCARGAPROCESO y estar activo (COCP_ACTUAL=1).
    /// </summary>
    [Required(ErrorMessage = "El eventoCargaProcesoId es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "El eventoCargaProcesoId debe ser mayor a 0")]
    public int EventoCargaProcesoId { get; set; }

    // NOTA: Los siguientes campos se calculan automáticamente y NO deben enviarse en el request:
    // - proceso: Se obtiene de CO_EVENTOSCARGAPROCESO.COCP_PROCESO mediante JOIN
    // - registrosSincronizados: Se obtiene de CO_EVENTOSCARGAPROCESO.COCP_REGISTROS
    // - dmsOrigen: Se consulta de CO_DISTRIBUIDORES usando dealerBac del JWT
    // - dealerBac: Se toma del JWT token
    // - nombreDealer: Se consulta de CO_DISTRIBUIDORES usando dealerBac del JWT
    // - fechaSincronizacion: Se calcula automáticamente con hora de México
    // - tokenConfirmacion: Se genera automáticamente con SHA256
    // - sincCargaProcesoDealerId: Se genera automáticamente por secuencia
    // - fechaAlta: SYSDATE
    // - usuarioAlta: Se toma del JWT token
}

/// <summary>
/// DTO para respuesta de sincronización de carga de proceso por dealer.
/// </summary>
public class SincCargaProcesoDealerDto
{
    public int SincCargaProcesoDealerId { get; set; }
    public string Proceso { get; set; } = string.Empty;
    public int EventoCargaProcesoId { get; set; }
    public string DmsOrigen { get; set; } = string.Empty;
    public string DealerBac { get; set; } = string.Empty;
    public string NombreDealer { get; set; } = string.Empty;
    public DateTime FechaSincronizacion { get; set; }
    public int RegistrosSincronizados { get; set; }
    public string TokenConfirmacion { get; set; } = string.Empty;
    /// <summary>
    /// Tiempo de sincronización en horas (diferencia entre FechaSincronizacion y FechaCarga).
    /// </summary>
    public decimal TiempoSincronizacionHoras { get; set; }
}

