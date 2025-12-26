using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

/// <summary>
/// Interfaz del repositorio de Distribuidor.
/// </summary>
public interface IDistribuidorRepository
{
    /// <summary>
    /// Obtiene un distribuidor por su código BAC (DEALERID).
    /// </summary>
    Task<Distribuidor?> ObtenerPorDealerBacAsync(string dealerBac);
}

