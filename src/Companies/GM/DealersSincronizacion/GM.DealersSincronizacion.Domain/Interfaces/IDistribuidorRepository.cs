using GM.DealersSincronizacion.Domain.Entities;

namespace GM.DealersSincronizacion.Domain.Interfaces;

/// <summary>
/// Interfaz del repositorio de Distribuidor.
/// </summary>
public interface IDistribuidorRepository
{
    /// <summary>
    /// Obtiene un distribuidor por su código BAC.
    /// </summary>
    Task<Distribuidor?> ObtenerPorDealerBacAsync(string dealerBac);
}

