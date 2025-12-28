using GM.DealersSincronizacion.Application.DTOs;

namespace GM.DealersSincronizacion.Application.Services;

/// <summary>
/// Interfaz del servicio de carga de archivo de sincronización para dealers.
/// </summary>
public interface ICargaArchivoSincService
{
    /// <summary>
    /// Obtiene el registro actual (actual=true) de carga de archivo de sincronización.
    /// </summary>
    Task<CargaArchivoSincActualDto?> ObtenerActualAsync();

    /// <summary>
    /// Obtiene el registro actual (actual=true) de carga de archivo de sincronización filtrado por proceso.
    /// </summary>
    /// <param name="proceso">Nombre del proceso (ej: "ProductList")</param>
    Task<CargaArchivoSincActualDto?> ObtenerActualPorProcesoAsync(string proceso);
}

