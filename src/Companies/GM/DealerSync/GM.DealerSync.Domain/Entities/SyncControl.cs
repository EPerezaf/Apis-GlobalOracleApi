namespace GM.DealerSync.Domain.Entities;

/// <summary>
/// Entidad que representa el control de ejecución de un proceso de sincronización batch.
/// Tabla: CO_EVENTOSCARGASINCCONTROL
/// </summary>
public class SyncControl
{
    /// <summary>
    /// Identificador único del registro (PK).
    /// Columna: COES_SINCCONTROLID
    /// </summary>
    public int SyncControlId { get; set; }

    /// <summary>
    /// Tipo de proceso (ProductList, CampaignList, etc.).
    /// Columna: COES_PROCESSTYPE
    /// </summary>
    public string ProcessType { get; set; } = string.Empty;

    /// <summary>
    /// ID de la carga del proceso.
    /// Columna: COES_IDCARGA
    /// </summary>
    public string IdCarga { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de carga del proceso.
    /// Columna: COES_FECHACARGA
    /// </summary>
    public DateTime FechaCarga { get; set; }

    /// <summary>
    /// Identificador del evento de carga de proceso (FK a CO_EVENTOSCARGAPROCESO).
    /// Columna: COES_COCP_EVENTPROCESOID
    /// </summary>
    public int? EventoCargaProcesoId { get; set; }

    /// <summary>
    /// JobId de Hangfire para tracking del job en background.
    /// Columna: COES_HANGFIREJOBID
    /// </summary>
    public string? HangfireJobId { get; set; }

    /// <summary>
    /// Estado del proceso (PENDING, RUNNING, COMPLETED, FAILED).
    /// Columna: COES_STATUS
    /// </summary>
    public string Status { get; set; } = "PENDING";

    /// <summary>
    /// Fecha de inicio del proceso.
    /// Columna: COES_FECHAINICIO
    /// </summary>
    public DateTime? FechaInicio { get; set; }

    /// <summary>
    /// Fecha de finalización del proceso.
    /// Columna: COES_FECHAFIN
    /// </summary>
    public DateTime? FechaFin { get; set; }

    /// <summary>
    /// Total de webhooks a procesar.
    /// Columna: COES_WEBHOOKSTOTALES
    /// </summary>
    public int WebhooksTotales { get; set; }

    /// <summary>
    /// Cantidad de webhooks procesados exitosamente.
    /// Columna: COES_WEBHOOKSPROCESADOS
    /// </summary>
    public int WebhooksProcesados { get; set; }

    /// <summary>
    /// Cantidad de webhooks que fallaron.
    /// Columna: COES_WEBHOOKSFALLIDOS
    /// </summary>
    public int WebhooksFallidos { get; set; }

    /// <summary>
    /// Cantidad de webhooks omitidos (ya sincronizados).
    /// Columna: COES_WEBHOOKSOMITIDOS
    /// </summary>
    public int WebhooksOmitidos { get; set; }

    /// <summary>
    /// Mensaje de error si el proceso falló.
    /// Columna: COES_ERRORMESSAGE
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Detalles adicionales del error (stack trace, etc.).
    /// Columna: COES_ERRORDETAILS
    /// </summary>
    public string? ErrorDetails { get; set; }

    // ========================================
    // CAMPOS DE AUDITORÍA
    // ========================================

    /// <summary>
    /// Fecha de creación del registro.
    /// Columna: FECHAREGISTRO
    /// </summary>
    public DateTime FechaRegistro { get; set; }

    /// <summary>
    /// Usuario que creó el registro.
    /// Columna: USUARIOREGISTRO
    /// </summary>
    public string? UsuarioRegistro { get; set; }

    /// <summary>
    /// Fecha de última modificación del registro.
    /// Columna: FECHAMODIFICACION
    /// </summary>
    public DateTime? FechaModificacion { get; set; }

    /// <summary>
    /// Usuario que realizó la última modificación.
    /// Columna: USUARIOMODIFICACION
    /// </summary>
    public string? UsuarioModificacion { get; set; }
}

