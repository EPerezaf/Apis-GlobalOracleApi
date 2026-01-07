using GM.DealerSync.Domain.Interfaces;

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
    /// <param name="lockWrapper">Lock adquirido que se liberará al finalizar</param>
    /// <param name="syncControlId">ID del registro en CO_EVENTOSCARGASINCCONTROL (opcional)</param>
    /// <param name="totalDealers">Total de dealers a procesar (opcional)</param>
    /// <returns>Task que representa la ejecución del proceso</returns>
    Task ExecuteBatchSyncAsync(string processId, string processType, string idCarga, IRedisLockWrapper lockWrapper, int? syncControlId = null, int totalDealers = 0);
    
    /// <summary>
    /// Ejecuta un proceso de sincronización batch usando Hangfire (wrapper para Hangfire)
    /// </summary>
    /// <param name="syncControlId">ID del registro en CO_EVENTOSCARGASINCCONTROL</param>
    /// <param name="processId">Identificador único del proceso</param>
    /// <param name="processType">Tipo de proceso</param>
    /// <param name="idCarga">ID de la carga</param>
    /// <returns>Task que representa la ejecución del proceso</returns>
    Task ExecuteBatchSyncWithHangfireAsync(int syncControlId, string processId, string processType, string idCarga);
}

