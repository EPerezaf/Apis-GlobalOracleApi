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

    /// <summary>
    /// Obtiene todos los distribuidores filtrados por empresa.
    /// </summary>
    /// <param name="empresaId">ID de la empresa (EMPR_EMPRESAID). Si es null, retorna todos los distribuidores.</param>
    /// <param name="usuario">Usuario para filtrar por CO_USUARIOXDEALER (opcional). Si se proporciona, solo retorna distribuidores asociados al usuario.</param>
    /// <returns>Lista de distribuidores</returns>
    Task<List<Distribuidor>> ObtenerTodosAsync(int? empresaId = null, string? usuario = null);
}

