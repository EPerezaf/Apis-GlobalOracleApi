using GM.CatalogSync.Domain.Entities;

namespace GM.DealersSincronizacion.Domain.Interfaces;

/// <summary>
/// Interfaz del repositorio para Sincronización de Archivos por Dealer (filtrado por dealerBac).
/// </summary>
public interface ISincArchivoDealerRepository
{
    /// <summary>
    /// Verifica si ya existe un registro de sincronización para el dealer y carga especificados.
    /// </summary>
    Task<SincArchivoDealer?> ObtenerPorCargaYDealerAsync(int cargaArchivoSincronizacionId, string dealerBac);

    /// <summary>
    /// Crea un nuevo registro de sincronización y actualiza los contadores en CO_CARGAARCHIVOSINCRONIZACION.
    /// </summary>
    Task<SincArchivoDealer> CrearAsync(SincArchivoDealer entidad, string usuarioAlta);
}



