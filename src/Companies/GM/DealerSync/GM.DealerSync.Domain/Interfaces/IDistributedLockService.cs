namespace GM.DealerSync.Domain.Interfaces;

/// <summary>
/// Servicio para manejo de distributed locks usando Redis RedLock
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    /// Intenta adquirir un lock distribuido para un proceso específico
    /// </summary>
    /// <param name="processType">Tipo de proceso (ej: "productList", "campaignList")</param>
    /// <param name="expiryTime">Tiempo de expiración del lock (por defecto: 1 minuto)</param>
    /// <returns>IDisposable del lock si se adquirió exitosamente, null si ya existe un lock activo</returns>
    Task<IDisposable?> TryAcquireLockAsync(string processType, TimeSpan? expiryTime = null);

    /// <summary>
    /// Verifica si existe un lock activo para un proceso específico
    /// </summary>
    /// <param name="processType">Tipo de proceso</param>
    /// <returns>True si existe un lock activo, False en caso contrario</returns>
    Task<bool> IsLockActiveAsync(string processType);
}

