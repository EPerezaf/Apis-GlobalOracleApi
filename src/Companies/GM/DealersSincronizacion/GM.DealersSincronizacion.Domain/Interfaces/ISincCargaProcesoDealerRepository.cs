using GM.CatalogSync.Domain.Entities;

namespace GM.DealersSincronizacion.Domain.Interfaces;

/// <summary>
/// Interfaz del repositorio para Sincronización de Carga de Proceso por Dealer (filtrado por dealerBac).
/// </summary>
public interface ISincCargaProcesoDealerRepository
{
    /// <summary>
    /// Verifica si ya existe un registro de sincronización para el dealer y evento de carga especificados.
    /// </summary>
    Task<SincCargaProcesoDealer?> ObtenerPorCargaYDealerAsync(int eventoCargaProcesoId, string dealerBac);

    /// <summary>
    /// Crea un nuevo registro de sincronización y actualiza los contadores en CO_EVENTOSCARGAPROCESO.
    /// </summary>
    Task<SincCargaProcesoDealer> CrearAsync(SincCargaProcesoDealer entidad, string usuarioAlta);
}

