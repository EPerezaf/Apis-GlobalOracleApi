using GM.DealerSync.Domain.Entities;

namespace GM.DealerSync.Domain.Interfaces;

/// <summary>
/// Interfaz para el repositorio de dealers
/// </summary>
public interface IDealerRepository
{
    /// <summary>
    /// Obtiene dealers activos agrupados por URLWebhook para un EventoCargaProcesoId.
    /// Filtra solo dealers con EstadoWebhook != 'EXITOSO' (Paso 14 del diagrama)
    /// </summary>
    Task<List<Dealer>> GetActiveDealersByProcessIdAsync(int eventoCargaProcesoId);

    /// <summary>
    /// Obtiene el EventoCargaProcesoId desde CO_EVENTOSCARGAPROCESO usando ProcessType e IdCarga
    /// </summary>
    Task<int?> GetEventoCargaProcesoIdAsync(string processType, string idCarga);

    /// <summary>
    /// Obtiene el EventoCargaProcesoId y FechaCarga desde CO_EVENTOSCARGAPROCESO usando ProcessType e IdCarga
    /// Retorna una tupla (EventoCargaProcesoId, FechaCarga)
    /// </summary>
    Task<(int EventoCargaProcesoId, DateTime FechaCarga)?> GetEventoCargaProcesoInfoAsync(string processType, string idCarga);

    /// <summary>
    /// Obtiene el Estatus de CO_EVENTOSCARGAPROCESO usando ProcessType e IdCarga
    /// </summary>
    Task<string?> GetEventoCargaProcesoEstatusAsync(string processType, string idCarga);

    /// <summary>
    /// Actualiza los contadores de dealers sincronizados y porcentaje en CO_EVENTOSCARGAPROCESO
    /// </summary>
    Task UpdateDealersSincronizadosAsync(int eventoCargaProcesoId, int dealersSincronizados, decimal porcentajeSincronizados, string currentUser);
}

