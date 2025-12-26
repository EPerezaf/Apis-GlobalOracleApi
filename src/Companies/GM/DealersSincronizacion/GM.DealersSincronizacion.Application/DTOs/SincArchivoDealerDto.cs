using System.ComponentModel.DataAnnotations;

namespace GM.DealersSincronizacion.Application.DTOs;

/// <summary>
/// DTO para creación de sincronización de archivo por dealer.
/// </summary>
public class CrearSincArchivoDealerDto
{
    /// <summary>
    /// ID de la carga de archivo de sincronización relacionada (FK).
    /// Debe existir en CO_CARGAARCHIVOSINCRONIZACION y estar activo (COCA_ACTUAL=1).
    /// </summary>
    [Required(ErrorMessage = "El cargaArchivoSincronizacionId es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "El cargaArchivoSincronizacionId debe ser mayor a 0")]
    public int CargaArchivoSincronizacionId { get; set; }

    // NOTA: Los siguientes campos se calculan automáticamente y NO deben enviarse en el request:
    // - proceso: Se obtiene de CO_CARGAARCHIVOSINCRONIZACION.COCA_PROCESO mediante JOIN
    // - registrosSincronizados: Se obtiene de CO_CARGAARCHIVOSINCRONIZACION.COCA_REGISTROS
    // - dmsOrigen: Se consulta de CO_DISTRIBUIDORES usando dealerBac del JWT
    // - dealerBac: Se toma del JWT token
    // - nombreDealer: Se consulta de CO_DISTRIBUIDORES usando dealerBac del JWT
    // - fechaSincronizacion: Se calcula automáticamente con hora de México
    // - sincArchivoDealerId: Se genera automáticamente por secuencia
    // - fechaAlta: SYSDATE
    // - usuarioAlta: Se toma del JWT token
}

/// <summary>
/// DTO para respuesta de sincronización de archivo por dealer.
/// </summary>
public class SincArchivoDealerDto
{
    public int SincArchivoDealerId { get; set; }
    public string Proceso { get; set; } = string.Empty;
    public int CargaArchivoSincronizacionId { get; set; }
    public string DmsOrigen { get; set; } = string.Empty;
    public string DealerBac { get; set; } = string.Empty;
    public string NombreDealer { get; set; } = string.Empty;
    public DateTime FechaSincronizacion { get; set; }
    public int RegistrosSincronizados { get; set; }
}



