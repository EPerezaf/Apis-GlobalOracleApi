using GM.DealerSync.Domain.Entities;

namespace GM.DealerSync.Domain.Interfaces;

/// <summary>
/// Interfaz para el repositorio de EventoCargaSnapshotDealer
/// </summary>
public interface IEventoCargaSnapshotDealerRepository
{
    /// <summary>
    /// Obtiene todos los dealers por UrlWebhook y EventoCargaProcesoId
    /// </summary>
    Task<List<EventoCargaSnapshotDealer>> GetDealersByUrlWebhookAsync(string urlWebhook, int eventoCargaProcesoId);

    /// <summary>
    /// Actualiza el estado de webhook a EXITOSO para dealers con UrlWebhook y EventoCargaProcesoId
    /// </summary>
    Task UpdateWebhookStatusToExitosoAsync(string urlWebhook, int eventoCargaProcesoId, string ackToken, string currentUser);

    /// <summary>
    /// Actualiza el estado de webhook a FALLIDO para dealers con UrlWebhook y EventoCargaProcesoId
    /// </summary>
    Task UpdateWebhookStatusToFallidoAsync(string urlWebhook, int eventoCargaProcesoId, string errorMessage, string currentUser);
}

