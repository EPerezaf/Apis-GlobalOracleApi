namespace GM.CatalogSync.Application.Exceptions;

/// <summary>
/// Excepción cuando no se encuentra un registro de carga de archivo de sincronización.
/// </summary>
public class CargaArchivoSincNoEncontradaException : Exception
{
    public CargaArchivoSincNoEncontradaException(int id)
        : base($"No se encontró el registro de carga de archivo con ID {id}")
    {
    }

    public CargaArchivoSincNoEncontradaException(string mensaje)
        : base(mensaje)
    {
    }
}

/// <summary>
/// Excepción cuando el ID de carga ya existe (violación de unicidad).
/// </summary>
public class IdCargaDuplicadoException : Exception
{
    public string IdCarga { get; }

    public IdCargaDuplicadoException(string idCarga)
        : base($"El ID de carga '{idCarga}' ya existe. El ID de carga debe ser único.")
    {
        IdCarga = idCarga;
    }
}

/// <summary>
/// Excepción para errores de validación en carga de archivos de sincronización.
/// </summary>
public class CargaArchivoSincValidacionException : Exception
{
    public CargaArchivoSincValidacionException(string mensaje)
        : base(mensaje)
    {
    }
}

