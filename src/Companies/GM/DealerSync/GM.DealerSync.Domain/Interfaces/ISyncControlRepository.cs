using GM.DealerSync.Domain.Entities;

namespace GM.DealerSync.Domain.Interfaces;

/// <summary>
/// Interfaz para el repositorio de control de sincronización
/// </summary>
public interface ISyncControlRepository
{
    /// <summary>
    /// Obtiene un registro de control por ID
    /// </summary>
    Task<SyncControl?> GetByIdAsync(int syncControlId);

    /// <summary>
    /// Obtiene un registro de control por ProcessType, IdCarga y FechaCarga
    /// </summary>
    Task<SyncControl?> GetByProcessAsync(string processType, string idCarga, DateTime fechaCarga);

    /// <summary>
    /// Obtiene todos los procesos pendientes (Paso 12 del diagrama)
    /// </summary>
    Task<List<SyncControl>> GetPendingProcessesAsync();

    /// <summary>
    /// Obtiene un proceso por Hangfire JobId
    /// </summary>
    Task<SyncControl?> GetByHangfireJobIdAsync(string hangfireJobId);

    /// <summary>
    /// Crea un nuevo registro de control de sincronización
    /// </summary>
    Task<SyncControl> CreateAsync(SyncControl syncControl, string currentUser);

    /// <summary>
    /// Actualiza un registro de control de sincronización
    /// </summary>
    Task<SyncControl> UpdateAsync(SyncControl syncControl, string currentUser);

    /// <summary>
    /// Actualiza el status a RUNNING con Hangfire JobId (Paso 9 del diagrama)
    /// </summary>
    Task UpdateStatusToRunningAsync(int syncControlId, string hangfireJobId, string currentUser);

    /// <summary>
    /// Actualiza el status a COMPLETED con estadísticas de webhooks
    /// </summary>
    Task UpdateStatusToCompletedAsync(int syncControlId, int webhooksProcesados, int webhooksFallidos, int webhooksOmitidos, string currentUser);

    /// <summary>
    /// Actualiza el status a FAILED con mensaje de error
    /// </summary>
    Task UpdateStatusToFailedAsync(int syncControlId, string errorMessage, string? errorDetails, string currentUser);
}

