namespace GM.DealersSincronizacion.Application.DTOs;

/// <summary>
/// DTO para el registro actual de carga de archivo de sincronización.
/// </summary>
public class CargaArchivoSincActualDto
{
    public int CargaArchivoSincronizacionId { get; set; }
    public string Proceso { get; set; } = string.Empty;
    public string NombreArchivo { get; set; } = string.Empty;
    public DateTime FechaCarga { get; set; }
    public string IdCarga { get; set; } = string.Empty;
    public int Registros { get; set; }
    public bool Actual { get; set; }
    // NOTA: Los campos DealersTotales, DealersSincronizados y PorcDealersSinc no se exponen a los dealers
}



