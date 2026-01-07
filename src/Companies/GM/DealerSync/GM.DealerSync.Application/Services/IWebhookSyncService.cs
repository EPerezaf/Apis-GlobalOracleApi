namespace GM.DealerSync.Application.Services;

/// <summary>
/// Resultado de la sincronización de webhook
/// </summary>
public class WebhookSyncResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string? AckToken { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsAuthError { get; set; }
    public bool IsConnectionError { get; set; }
}

/// <summary>
/// Interfaz para el servicio de sincronización de webhooks
/// </summary>
public interface IWebhookSyncService
{
    /// <summary>
    /// Envía un webhook al dealer con autenticación
    /// </summary>
    /// <param name="urlWebhook">URL del webhook</param>
    /// <param name="secretKey">Secret key para autenticación</param>
    /// <param name="payload">Payload a enviar</param>
    /// <returns>Resultado de la sincronización</returns>
    Task<WebhookSyncResult> SendWebhookAsync(string urlWebhook, string? secretKey, object payload);
}

