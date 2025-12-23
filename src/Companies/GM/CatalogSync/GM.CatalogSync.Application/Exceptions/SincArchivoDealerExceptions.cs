namespace GM.CatalogSync.Application.Exceptions;

/// <summary>
/// Excepción cuando ya existe un registro con la misma combinación proceso, cargaArchivoSincronizacionId y dealerBac.
/// </summary>
public class SincArchivoDealerDuplicadoException : Exception
{
    public string Proceso { get; }
    public int CargaArchivoSincronizacionId { get; }
    public string DealerBac { get; }

    public SincArchivoDealerDuplicadoException(string proceso, int cargaArchivoSincronizacionId, string dealerBac)
        : base($"Ya existe un registro para el proceso '{proceso}', cargaArchivoSincronizacionId '{cargaArchivoSincronizacionId}' y dealer '{dealerBac}'")
    {
        Proceso = proceso;
        CargaArchivoSincronizacionId = cargaArchivoSincronizacionId;
        DealerBac = dealerBac;
    }
}

/// <summary>
/// Excepción para errores de validación en SincArchivoDealer.
/// </summary>
public class SincArchivoDealerValidacionException : Exception
{
    public SincArchivoDealerValidacionException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// Excepción cuando no se encuentra un registro de SincArchivoDealer.
/// </summary>
public class SincArchivoDealerNotFoundException : Exception
{
    public int Id { get; }

    public SincArchivoDealerNotFoundException(int id)
        : base($"No se encontró el registro de sincronización con ID {id}")
    {
        Id = id;
    }
}
