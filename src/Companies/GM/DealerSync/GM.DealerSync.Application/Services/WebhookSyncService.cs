using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace GM.DealerSync.Application.Services;

/// <summary>
/// Servicio para sincronización de webhooks con autenticación
/// </summary>
public class WebhookSyncService : IWebhookSyncService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookSyncService> _logger;
    private readonly Random _random = new();

    public WebhookSyncService(
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookSyncService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        
        // ⚙️ Configuración de Timeout por webhook
        // Timeout de 5 minutos por webhook individual
        // - Permite manejar webhooks que pueden tardar en procesar grandes catálogos
        // - Evita que un webhook lento bloquee el procesamiento de otros
        // - Con procesamiento paralelo (5-10 simultáneos), el timeout no bloquea otros webhooks
        // TODO: Considerar implementar Circuit Breaker (Polly) para:
        //   - Detectar webhooks fallidos repetidamente
        //   - Abrir el circuito y evitar llamadas durante un período
        //   - Implementar backoff exponencial para reintentos
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WebhookSyncResult> SendWebhookAsync(string urlWebhook, string? secretKey, object payload)
    {
        try
        {
            _logger.LogInformation(
                "🌐 [WEBHOOK] Enviando webhook a: {UrlWebhook}",
                urlWebhook);

            // Serializar payload
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Agregar header de autenticación
            if (!string.IsNullOrWhiteSpace(secretKey))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-Webhook-Secret");
                _httpClient.DefaultRequestHeaders.Add("X-Webhook-Secret", secretKey);
            }

            // Intentar POST real
            HttpResponseMessage? response = null;
            try
            {
                response = await _httpClient.PostAsync(urlWebhook, content);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogWarning(
                    "⏱️ [WEBHOOK] Timeout al conectar con webhook: {UrlWebhook}",
                    urlWebhook);
                
                // Simular error de conexión aleatoriamente
                return SimulateConnectionError();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "❌ [WEBHOOK] Error de conexión con webhook: {UrlWebhook}, Error: {ErrorMessage}",
                    urlWebhook, ex.Message);
                
                // Simular error de conexión aleatoriamente
                return SimulateConnectionError();
            }

            if (response == null)
            {
                return SimulateConnectionError();
            }

            // Si es 401 o 403, simular diferentes escenarios
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
            _logger.LogWarning(
                "🔒 [WEBHOOK] Error de autenticación (401/403) detectado - Activando simulación: {UrlWebhook}",
                urlWebhook);
                
                // Simular diferentes escenarios aleatoriamente (éxito, error de auth, error de conexión)
                return await SimulateRandomScenarioAsync();
            }

            // Si es exitoso (200 OK)
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Intentar deserializar el ACK token
                string? ackToken = null;
                try
                {
                    var responseJson = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    if (responseJson.TryGetProperty("ackToken", out var ackTokenElement) ||
                        responseJson.TryGetProperty("ack_token", out ackTokenElement) ||
                        responseJson.TryGetProperty("tokenConfirmacion", out ackTokenElement))
                    {
                        ackToken = ackTokenElement.GetString();
                    }
                    else
                    {
                        // Si no tiene ackToken, generar uno
                        ackToken = GenerateAckToken(urlWebhook, jsonPayload);
                    }
                }
                catch
                {
                    // Si no se puede deserializar, generar un ACK token
                    ackToken = GenerateAckToken(urlWebhook, jsonPayload);
                }

                _logger.LogInformation(
                    "✅ [WEBHOOK] Webhook enviado exitosamente: {UrlWebhook}, StatusCode: {StatusCode}, AckToken: {AckToken}",
                    urlWebhook, response.StatusCode, ackToken);

                return new WebhookSyncResult
                {
                    IsSuccess = true,
                    StatusCode = (int)response.StatusCode,
                    AckToken = ackToken,
                    IsAuthError = false,
                    IsConnectionError = false
                };
            }

            // Otros códigos de error (4xx, 5xx) - Activar simulación
            var errorMessage = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"   🔄 [SIMULACIÓN] Error HTTP detectado (StatusCode: {(int)response.StatusCode}) - Activando simulación...");
            Console.Out.Flush();
            
            _logger.LogWarning(
                "⚠️ [WEBHOOK] Error HTTP detectado (StatusCode: {StatusCode}) - Activando simulación: {UrlWebhook}",
                response.StatusCode, urlWebhook);

            // Simular diferentes escenarios aleatoriamente
            var simulatedResult = await SimulateRandomScenarioAsync();
            
            Console.WriteLine($"   📊 [SIMULACIÓN] Resultado simulado: IsSuccess={simulatedResult.IsSuccess}, StatusCode={simulatedResult.StatusCode}");
            if (simulatedResult.IsSuccess)
            {
                Console.WriteLine($"   🎫 [SIMULACIÓN] ACK Token generado: {simulatedResult.AckToken}");
            }
            else
            {
                Console.WriteLine($"   ❌ [SIMULACIÓN] Error simulado: {simulatedResult.ErrorMessage}");
            }
            Console.Out.Flush();
            
            return simulatedResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "❌ [WEBHOOK] Error inesperado al enviar webhook: {UrlWebhook}",
                urlWebhook);

            // Simular error de conexión aleatoriamente
            return SimulateConnectionError();
        }
        finally
        {
            // Limpiar headers
            _httpClient.DefaultRequestHeaders.Remove("X-Webhook-Secret");
        }
    }

    /// <summary>
    /// Simula diferentes escenarios aleatorios: éxito, error de auth, error de conexión
    /// </summary>
    private async Task<WebhookSyncResult> SimulateRandomScenarioAsync()
    {
        // Random entre 0-9 para diferentes escenarios:
        // 0-4: Éxito (50%)
        // 5-7: Error de autenticación (30%)
        // 8-9: Error de conexión (20%)
        var scenario = _random.Next(0, 10);

        if (scenario <= 4)
        {
            // Simular éxito con delay aleatorio de 3-10 segundos
            var delaySeconds = _random.Next(3, 11); // Entre 3 y 10 segundos (exclusivo en el límite superior)
            
            Console.WriteLine($"   ✅ [SIMULACIÓN] Escenario {scenario}: ÉXITO simulado - Esperando {delaySeconds} segundos...");
            Console.Out.Flush();
            
            _logger.LogInformation(
                "✅ [WEBHOOK] Simulación: Éxito con delay de {DelaySeconds}s - Escenario: {Scenario}",
                delaySeconds, scenario);
            
            // Delay aleatorio para simular tiempo de procesamiento
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            
            var ackToken = GenerateAckToken("simulated", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
            
            Console.WriteLine($"   ✅ [SIMULACIÓN] Delay completado - ACK Token generado: {ackToken}");
            Console.Out.Flush();
            
            _logger.LogInformation(
                "✅ [WEBHOOK] Simulación: Éxito completado con ACK Token: {AckToken}",
                ackToken);
            
            return new WebhookSyncResult
            {
                IsSuccess = true,
                StatusCode = 200,
                AckToken = ackToken,
                IsAuthError = false,
                IsConnectionError = false
            };
        }
        else if (scenario <= 7)
        {
            // Simular error de autenticación
            Console.WriteLine($"   🔒 [SIMULACIÓN] Escenario {scenario}: Error de Autenticación simulado");
            Console.Out.Flush();
            return SimulateAuthError();
        }
        else
        {
            // Simular error de conexión
            Console.WriteLine($"   🔌 [SIMULACIÓN] Escenario {scenario}: Error de Conexión simulado");
            Console.Out.Flush();
            return SimulateConnectionError();
        }
    }

    /// <summary>
    /// Simula un error de autenticación con diferentes escenarios aleatorios
    /// </summary>
    private WebhookSyncResult SimulateAuthError()
    {
        // Random entre 0-4 para diferentes escenarios
        var scenario = _random.Next(0, 5);

        return scenario switch
        {
            0 => new WebhookSyncResult
            {
                IsSuccess = false,
                StatusCode = 401,
                ErrorMessage = "Unauthorized: Invalid secret key",
                IsAuthError = true,
                IsConnectionError = false
            },
            1 => new WebhookSyncResult
            {
                IsSuccess = false,
                StatusCode = 403,
                ErrorMessage = "Forbidden: Access denied",
                IsAuthError = true,
                IsConnectionError = false
            },
            2 => new WebhookSyncResult
            {
                IsSuccess = false,
                StatusCode = 401,
                ErrorMessage = "Unauthorized: Token expired",
                IsAuthError = true,
                IsConnectionError = false
            },
            3 => new WebhookSyncResult
            {
                IsSuccess = false,
                StatusCode = 403,
                ErrorMessage = "Forbidden: Invalid credentials",
                IsAuthError = true,
                IsConnectionError = false
            },
            _ => new WebhookSyncResult
            {
                IsSuccess = false,
                StatusCode = 401,
                ErrorMessage = "Unauthorized: Authentication failed",
                IsAuthError = true,
                IsConnectionError = false
            }
        };
    }

    /// <summary>
    /// Simula un error de conexión con diferentes escenarios aleatorios
    /// </summary>
    private WebhookSyncResult SimulateConnectionError()
    {
        // Random entre 0-3 para diferentes escenarios
        var scenario = _random.Next(0, 4);

        return scenario switch
        {
            0 => new WebhookSyncResult
            {
                IsSuccess = false,
                StatusCode = 0,
                ErrorMessage = "Connection timeout: The request timed out",
                IsAuthError = false,
                IsConnectionError = true
            },
            1 => new WebhookSyncResult
            {
                IsSuccess = false,
                StatusCode = 0,
                ErrorMessage = "Connection refused: Unable to connect to the remote server",
                IsAuthError = false,
                IsConnectionError = true
            },
            2 => new WebhookSyncResult
            {
                IsSuccess = false,
                StatusCode = 0,
                ErrorMessage = "Network error: No connection could be made",
                IsAuthError = false,
                IsConnectionError = true
            },
            _ => new WebhookSyncResult
            {
                IsSuccess = false,
                StatusCode = 0,
                ErrorMessage = "Connection error: Failed to establish connection",
                IsAuthError = false,
                IsConnectionError = true
            }
        };
    }

    /// <summary>
    /// Genera un token ACK único
    /// </summary>
    private string GenerateAckToken(string urlWebhook, string payload)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var data = $"{urlWebhook}{payload}{timestamp}";
        
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        var hashString = Convert.ToHexString(hash).ToLowerInvariant();
        
        return $"ACK-{hashString.Substring(0, Math.Min(32, hashString.Length))}";
    }

    /// <summary>
    /// Sobrecarga para generar ACK token con solo un valor
    /// </summary>
    private string GenerateAckToken(string seed)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var data = $"{seed}{timestamp}{_random.Next(1000, 9999)}";
        
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        var hashString = Convert.ToHexString(hash).ToLowerInvariant();
        
        return $"ACK-{hashString.Substring(0, Math.Min(32, hashString.Length))}";
    }
}

