namespace GM.DealerSync.Application.Services;

/// <summary>
/// Servicio para manejo de jobs de sincronización batch
/// </summary>
public interface IBatchSyncJobService
{
    /// <summary>
    /// Ejecuta un proceso de sincronización batch
    /// </summary>
    /// <param name="processId">Identificador único del proceso</param>
    /// <param name="processType">Tipo de proceso</param>
    /// <param name="idCarga">ID de la carga</param>
    /// <param name="lockDisposable">Lock adquirido que se liberará al finalizar</param>
    /// <returns>Task que representa la ejecución del proceso</returns>
    Task ExecuteBatchSyncAsync(string processId, string processType, string idCarga, IDisposable lockDisposable);
    
    /// <summary>
    /// Ejecuta un proceso de sincronización batch usando Hangfire (wrapper para Hangfire)
    /// </summary>
    /// <param name="processId">Identificador único del proceso</param>
    /// <param name="processType">Tipo de proceso</param>
    /// <param name="idCarga">ID de la carga</param>
    /// <returns>Task que representa la ejecución del proceso</returns>
    Task ExecuteBatchSyncWithHangfireAsync(string processId, string processType, string idCarga);
}

