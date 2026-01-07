using System.ComponentModel.DataAnnotations;

namespace GM.DealerSync.Application.DTOs;

/// <summary>
/// DTO para solicitud de sincronización batch de procesos
/// </summary>
public class BatchSincronizacionProcesosDto
{
    /// <summary>
    /// Tipo de proceso a sincronizar (ej: "productList", "campaignList")
    /// </summary>
    [Required(ErrorMessage = "El processType es requerido")]
    [StringLength(50, ErrorMessage = "El processType no puede exceder 50 caracteres")]
    public string ProcessType { get; set; } = string.Empty;

    /// <summary>
    /// ID de la carga del proceso
    /// </summary>
    [Required(ErrorMessage = "El idCarga es requerido")]
    [StringLength(100, ErrorMessage = "El idCarga no puede exceder 100 caracteres")]
    public string IdCarga { get; set; } = string.Empty;
}

/// <summary>
/// DTO para respuesta de sincronización batch de procesos
/// </summary>
public class BatchSincronizacionProcesosResponseDto
{
    /// <summary>
    /// Identificador único del proceso (ProcessId)
    /// </summary>
    public string ProcessId { get; set; } = string.Empty;

    /// <summary>
    /// Indica si el lock fue adquirido exitosamente
    /// </summary>
    public bool LockAcquired { get; set; }

    /// <summary>
    /// Tipo de proceso
    /// </summary>
    public string ProcessType { get; set; } = string.Empty;

    /// <summary>
    /// ID de la carga
    /// </summary>
    public string IdCarga { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje descriptivo del resultado
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp de inicio del proceso
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Tiempo estimado de duración del lock (en segundos)
    /// </summary>
    public int LockExpirySeconds { get; set; }
}

