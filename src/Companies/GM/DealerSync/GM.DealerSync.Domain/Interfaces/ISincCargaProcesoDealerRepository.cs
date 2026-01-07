using GM.DealerSync.Domain.Entities;

namespace GM.DealerSync.Domain.Interfaces;

/// <summary>
/// Interfaz para el repositorio de SincCargaProcesoDealer
/// </summary>
public interface ISincCargaProcesoDealerRepository
{
    /// <summary>
    /// Obtiene el conteo de registros sincronizados para un EventoCargaProcesoId
    /// </summary>
    Task<int> GetCountByEventoCargaProcesoIdAsync(int eventoCargaProcesoId);

    /// <summary>
    /// Crea un nuevo registro de sincronización
    /// </summary>
    Task<SincCargaProcesoDealer> CreateAsync(SincCargaProcesoDealer entidad, string usuarioAlta);

    /// <summary>
    /// Obtiene el conteo total de dealers en EventoCargaSnapshotDealers para un EventoCargaProcesoId
    /// </summary>
    Task<int> GetTotalDealersCountAsync(int eventoCargaProcesoId);
}

