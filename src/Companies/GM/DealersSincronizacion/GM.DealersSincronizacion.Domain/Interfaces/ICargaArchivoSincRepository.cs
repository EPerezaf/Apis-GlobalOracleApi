using GM.CatalogSync.Domain.Entities;

namespace GM.DealersSincronizacion.Domain.Interfaces;

/// <summary>
/// Interfaz del repositorio para Carga de Archivo de Sincronización (filtrado por dealer).
/// </summary>
public interface ICargaArchivoSincRepository
{
    /// <summary>
    /// Obtiene el registro actual (actual=true) de carga de archivo de sincronización.
    /// </summary>
    Task<CargaArchivoSincronizacion?> ObtenerActualAsync();

    /// <summary>
    /// Obtiene el registro actual (actual=true) de carga de archivo de sincronización filtrado por proceso.
    /// </summary>
    /// <param name="proceso">Nombre del proceso (ej: "ProductList")</param>
    Task<CargaArchivoSincronizacion?> ObtenerActualPorProcesoAsync(string proceso);
}



