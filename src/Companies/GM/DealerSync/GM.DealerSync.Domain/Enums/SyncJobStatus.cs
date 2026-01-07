namespace GM.DealerSync.Domain.Enums;

/// <summary>
/// Enum para representar el estado de un job de sincronización
/// </summary>
public enum SyncJobStatus
{
    /// <summary>
    /// Proceso pendiente de ejecución
    /// </summary>
    PENDING,

    /// <summary>
    /// Proceso en ejecución
    /// </summary>
    RUNNING,

    /// <summary>
    /// Proceso completado exitosamente
    /// </summary>
    COMPLETED,

    /// <summary>
    /// Proceso fallido
    /// </summary>
    FAILED
}

