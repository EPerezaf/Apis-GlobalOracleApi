namespace GM.DealerSync.Domain.Interfaces;

/// <summary>
/// Wrapper del lock de Redis que permite renovación
/// </summary>
public interface IRedisLockWrapper : IDisposable
{
    /// <summary>
    /// Tipo de proceso para el cual se adquirió el lock
    /// </summary>
    string ProcessType { get; }

    /// <summary>
    /// Valor único del lock (usado para verificar propiedad al renovar o liberar)
    /// </summary>
    string LockValue { get; }
}

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
    /// <returns>IRedisLockWrapper del lock si se adquirió exitosamente, null si ya existe un lock activo</returns>
    Task<IRedisLockWrapper?> TryAcquireLockAsync(string processType, TimeSpan? expiryTime = null);

    /// <summary>
    /// Verifica si existe un lock activo para un proceso específico
    /// </summary>
    /// <param name="processType">Tipo de proceso</param>
    /// <returns>True si existe un lock activo, False en caso contrario</returns>
    Task<bool> IsLockActiveAsync(string processType);

    /// <summary>
    /// Renueva el tiempo de expiración de un lock existente (heartbeat)
    /// </summary>
    /// <param name="processType">Tipo de proceso</param>
    /// <param name="lockValue">Valor del lock que se desea renovar</param>
    /// <param name="newExpiryTime">Nuevo tiempo de expiración</param>
    /// <returns>True si el lock se renovó exitosamente, False si el lock no existe o fue reemplazado</returns>
    Task<bool> RenewLockAsync(string processType, string lockValue, TimeSpan newExpiryTime);
}

