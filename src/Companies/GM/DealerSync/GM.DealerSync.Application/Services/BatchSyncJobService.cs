using GM.DealerSync.Domain.Entities;
using GM.DealerSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Security;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;

namespace GM.DealerSync.Application.Services;

/// <summary>
/// Implementación del servicio de jobs de sincronización batch
/// </summary>
public class BatchSyncJobService : IBatchSyncJobService
{
    private readonly ILogger<BatchSyncJobService> _logger;
    private readonly ISyncControlRepository _syncControlRepository;
    private readonly IDealerRepository _dealerRepository;
    private readonly IDistributedLockService _distributedLockService;
    private readonly IWebhookSyncService _webhookSyncService;
    private readonly IEventoCargaSnapshotDealerRepository _eventoCargaSnapshotDealerRepository;
    private readonly ISincCargaProcesoDealerRepository _sincCargaProcesoDealerRepository;
    private const int SIMULATED_PROCESSING_SECONDS = 600; // 10 minutos de procesamiento
    private const int LOG_INTERVAL_SECONDS = 5; // Log cada 5 segundos
    private const int LOCK_RENEWAL_INTERVAL_SECONDS = 30; // Renovar lock cada 30 segundos (heartbeat)
    private const int LOCK_RENEWAL_EXPIRY_SECONDS = 600; // Renovar por 10 minutos adicionales (el lock nunca expirará mientras el proceso esté activo)
    
    // Almacenar locks por processType para que Hangfire pueda acceder a ellos
    private static readonly ConcurrentDictionary<string, IRedisLockWrapper> _activeLocks = new();

    public BatchSyncJobService(
        ILogger<BatchSyncJobService> logger,
        ISyncControlRepository syncControlRepository,
        IDealerRepository dealerRepository,
        IDistributedLockService distributedLockService,
        IWebhookSyncService webhookSyncService,
        IEventoCargaSnapshotDealerRepository eventoCargaSnapshotDealerRepository,
        ISincCargaProcesoDealerRepository sincCargaProcesoDealerRepository)
    {
        _logger = logger;
        _syncControlRepository = syncControlRepository;
        _dealerRepository = dealerRepository;
        _distributedLockService = distributedLockService;
        _webhookSyncService = webhookSyncService;
        _eventoCargaSnapshotDealerRepository = eventoCargaSnapshotDealerRepository;
        _sincCargaProcesoDealerRepository = sincCargaProcesoDealerRepository;
    }

    /// <inheritdoc />
    public async Task ExecuteBatchSyncAsync(string processId, string processType, string idCarga, IRedisLockWrapper lockWrapper, int? syncControlId = null, int totalDealers = 0)
    {
        // Enriquecer el contexto de logging con ProcessId único
        using (_logger.BeginScope(new Dictionary<string, object> { ["ProcessId"] = processId }))
        {
            var startTime = DateTimeHelper.GetMexicoDateTime();
            var startTimeString = DateTimeHelper.GetMexicoTimeString();
            
            try
            {
                // Log simplificado en consola
                Console.WriteLine("🚀 [BATCH_SYNC] INICIANDO proceso de sincronización batch");
                Console.WriteLine($"⏱️ [BATCH_SYNC] Duración estimada: {SIMULATED_PROCESSING_SECONDS} segundos ({SIMULATED_PROCESSING_SECONDS / 60} minutos)");
                Console.WriteLine($"🔄 [REDIS_LOCK] Heartbeat activo: renovación cada {LOCK_RENEWAL_INTERVAL_SECONDS}s por {LOCK_RENEWAL_EXPIRY_SECONDS / 60} minutos adicionales");
                Console.Out.Flush();
                
                // Log detallado solo en Serilog (no duplicar en consola)
                _logger.LogInformation(
                    "🚀 [BATCH_SYNC] INICIANDO proceso de sincronización batch | ProcessId: {ProcessId}, ProcessType: {ProcessType}, IdCarga: {IdCarga}, StartTime: {StartTime}",
                    processId, processType, idCarga, startTimeString);

            // Procesar dealers (simulado por ahora, pero con información real)
            int dealersProcesados = 0;
            
            if (totalDealers > 0)
            {
                Console.WriteLine($"🔄 [BATCH_SYNC] Procesando {totalDealers} dealers...");
                Console.Out.Flush();
                
                // Dividir el procesamiento en intervalos para mostrar progreso
                var totalIntervals = SIMULATED_PROCESSING_SECONDS / LOG_INTERVAL_SECONDS;
                var dealersPorIntervalo = Math.Max(1, totalDealers / totalIntervals);

                for (int i = 1; i <= totalIntervals; i++)
                {
                    // Si ya procesamos todos los dealers, terminar el proceso inmediatamente
                    if (dealersProcesados >= totalDealers)
                    {
                        Console.WriteLine($"✅ [BATCH_SYNC] Todos los dealers procesados ({dealersProcesados}/{totalDealers}). Terminando proceso...");
                        Console.Out.Flush();
                        _logger.LogInformation(
                            "✅ [BATCH_SYNC] Todos los dealers procesados. Terminando proceso anticipadamente. DealersProcesados: {DealersProcesados}/{TotalDealers}",
                            dealersProcesados, totalDealers);
                        break; // Salir del loop
                    }
                    
                    await Task.Delay(TimeSpan.FromSeconds(LOG_INTERVAL_SECONDS));
                    
                    var currentTime = DateTimeHelper.GetMexicoDateTime();
                    var elapsed = (currentTime - startTime).TotalSeconds;
                    var remaining = SIMULATED_PROCESSING_SECONDS - elapsed;
                    var progressPercent = Math.Round((elapsed / SIMULATED_PROCESSING_SECONDS) * 100, 1);
                    
                    // Simular procesamiento de dealers en este intervalo
                    var dealersEnEsteIntervalo = Math.Min(dealersPorIntervalo, totalDealers - dealersProcesados);
                    dealersProcesados += dealersEnEsteIntervalo;
                    
                    // Asegurar que no exceda el total
                    if (dealersProcesados > totalDealers)
                    {
                        dealersProcesados = totalDealers;
                    }
                    
                    // Log simplificado en consola cada 5 segundos con información de dealers
                    var progresoMsg = $"⏳ [BATCH_SYNC] Ejecutando proceso... [{(int)elapsed}s / {SIMULATED_PROCESSING_SECONDS}s] - Progreso: {progressPercent}% | Dealers procesados: {dealersProcesados}/{totalDealers}";
                    Console.WriteLine(progresoMsg);
                    Console.Out.Flush();
                    
                    // Log detallado solo en Serilog
                    _logger.LogInformation(
                        "⏳ [BATCH_SYNC] Ejecutando proceso... [{ElapsedSeconds}s / {TotalSeconds}s] - Progreso: {ProgressPercent}% | Restante: {RemainingSeconds}s | Dealers procesados: {DealersProcesados}/{TotalDealers} | ProcessType: {ProcessType}, IdCarga: {IdCarga}, ProcessId: {ProcessId}",
                        elapsed, SIMULATED_PROCESSING_SECONDS, progressPercent, remaining, dealersProcesados, totalDealers, processType, idCarga, processId);
                    
                    // Si ya procesamos todos los dealers en este intervalo, terminar después del log
                    if (dealersProcesados >= totalDealers)
                    {
                        Console.WriteLine($"✅ [BATCH_SYNC] Todos los dealers procesados ({dealersProcesados}/{totalDealers}). Terminando proceso...");
                        Console.Out.Flush();
                        _logger.LogInformation(
                            "✅ [BATCH_SYNC] Todos los dealers procesados. Terminando proceso anticipadamente. DealersProcesados: {DealersProcesados}/{TotalDealers}",
                            dealersProcesados, totalDealers);
                        break; // Salir del loop
                    }
                }
                
                // Asegurar que todos los dealers se marquen como procesados (por si acaso)
                if (dealersProcesados < totalDealers)
                {
                    dealersProcesados = totalDealers;
                }
            }
            else
            {
                // Si no hay dealers, usar el comportamiento anterior
                var totalIntervals = SIMULATED_PROCESSING_SECONDS / LOG_INTERVAL_SECONDS;

                for (int i = 1; i <= totalIntervals; i++)
                {
                    await Task.Delay(TimeSpan.FromSeconds(LOG_INTERVAL_SECONDS));
                    
                    var currentTime = DateTimeHelper.GetMexicoDateTime();
                    var elapsed = (currentTime - startTime).TotalSeconds;
                    var remaining = SIMULATED_PROCESSING_SECONDS - elapsed;
                    var progressPercent = Math.Round((elapsed / SIMULATED_PROCESSING_SECONDS) * 100, 1);
                    
                    // Log simplificado en consola cada 5 segundos
                    var progresoMsg = $"⏳ [BATCH_SYNC] Ejecutando proceso... [{(int)elapsed}s / {SIMULATED_PROCESSING_SECONDS}s] - Progreso: {progressPercent}%";
                    Console.WriteLine(progresoMsg);
                    Console.Out.Flush();
                    
                    // Log detallado solo en Serilog
                    _logger.LogInformation(
                        "⏳ [BATCH_SYNC] Ejecutando proceso... [{ElapsedSeconds}s / {TotalSeconds}s] - Progreso: {ProgressPercent}% | Restante: {RemainingSeconds}s | ProcessType: {ProcessType}, IdCarga: {IdCarga}, ProcessId: {ProcessId}",
                        elapsed, SIMULATED_PROCESSING_SECONDS, progressPercent, remaining, processType, idCarga, processId);
                }
            }

                var endTime = DateTimeHelper.GetMexicoDateTime();
                var endTimeString = DateTimeHelper.GetMexicoTimeString();
                var duration = (endTime - startTime).TotalSeconds;
                var durationMinutes = Math.Round(duration / 60.0, 2);

                // Actualizar status a COMPLETED si tenemos syncControlId
                if (syncControlId.HasValue)
                {
                    try
                    {
                        // Este código está obsoleto - la actualización se hace en ProcessDealersWebhooksAsync
                        // Mantenemos esto por compatibilidad pero debería estar vacío o eliminarse
                        _logger.LogWarning(
                            "⚠️ [BATCH_SYNC] Se intentó actualizar status en método obsoleto. La actualización se realiza en ProcessDealersWebhooksAsync");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ [BATCH_SYNC] Error al actualizar status a COMPLETED: {ex.Message}");
                        Console.Out.Flush();
                        
                        _logger.LogError(ex,
                            "⚠️ [BATCH_SYNC] Error al actualizar status a COMPLETED. SyncControlId: {SyncControlId}",
                            syncControlId.Value);
                    }
                }

                // Log simplificado de finalización en consola
                Console.WriteLine("✅ [BATCH_SYNC] PROCESO FINALIZADO");
                Console.Out.Flush();
                
                // Log detallado solo en Serilog
                _logger.LogInformation(
                    "✅ [BATCH_SYNC] PROCESO FINALIZADO | ProcessId: {ProcessId}, ProcessType: {ProcessType}, IdCarga: {IdCarga}, StartTime: {StartTime}, EndTime: {EndTime}, Duración: {DurationSeconds}s ({DurationMinutes} min), TotalDealers: {TotalDealers}",
                    processId, processType, idCarga, startTimeString, endTimeString, duration, durationMinutes, totalDealers);
        }
            catch (Exception ex)
            {
                var errorTime = DateTimeHelper.GetMexicoDateTime();
                var elapsed = (errorTime - startTime).TotalSeconds;
                
                var errorMsg1 = $"❌ [BATCH_SYNC] ════════════════════════════════════════════════════════════";
                Console.WriteLine(errorMsg1);
                _logger.LogError(ex, errorMsg1);
                
                var errorMsg2 = $"❌ [BATCH_SYNC] ERROR durante el proceso de sincronización batch | ProcessId: {processId}";
                Console.WriteLine(errorMsg2);
                _logger.LogError(
                    "❌ [BATCH_SYNC] ERROR durante el proceso de sincronización batch | ProcessId: {ProcessId}",
                    processId);
                
                var errorMsg3 = $"❌ [BATCH_SYNC] ProcessType: {processType}, IdCarga: {idCarga}, Elapsed: {elapsed}s, ProcessId: {processId}";
                Console.WriteLine(errorMsg3);
                _logger.LogError(
                    "❌ [BATCH_SYNC] ProcessType: {ProcessType}, IdCarga: {IdCarga}, Elapsed: {ElapsedSeconds}s, ProcessId: {ProcessId}",
                    processType, idCarga, elapsed, processId);
                
                var errorMsg4 = $"❌ [BATCH_SYNC] Excepción: {ex.Message}";
                Console.WriteLine(errorMsg4);
                _logger.LogError(ex,
                    "❌ [BATCH_SYNC] Excepción: {ExceptionMessage}",
                    ex.Message);
                
                var errorMsg5 = $"❌ [BATCH_SYNC] ════════════════════════════════════════════════════════════";
                Console.WriteLine(errorMsg5);
                _logger.LogError(errorMsg5);
                
                // Actualizar status a FAILED si tenemos syncControlId
                if (syncControlId.HasValue)
                {
                    try
                    {
                        await _syncControlRepository.UpdateStatusToFailedAsync(
                            syncControlId.Value,
                            ex.Message,
                            ex.ToString(),
                            "SYSTEM");
                    }
                    catch { /* Ignorar error de actualización */ }
                }
                
                throw;
            }
            finally
            {
                var finalTime = DateTimeHelper.GetMexicoDateTime();
                var totalDuration = (finalTime - startTime).TotalSeconds;
                var totalDurationMinutes = Math.Round(totalDuration / 60.0, 2);
                
                // Log detallado solo en Serilog antes de liberar
                _logger.LogInformation(
                    "🔓 [BATCH_SYNC] PROCESO TERMINADO | ProcessId: {ProcessId}, ProcessType: {ProcessType}, IdCarga: {IdCarga}, Duración: {DurationSeconds}s",
                    processId, processType, idCarga, totalDuration);
                
                // Liberar el lock al finalizar (esto mostrará logs del RedisLockWrapper)
                lockWrapper?.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public async Task ExecuteBatchSyncWithHangfireAsync(int syncControlId, string processId, string processType, string idCarga)
    {
        // Paso 12: Obtener el registro de SyncControl desde CO_EVENTOSCARGASINCCONTROL por ID
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine($"📋 [BATCH_SYNC] Paso 12: Obteniendo registro SyncControlId: {syncControlId}...");
        Console.Out.Flush();
        
        var syncControl = await _syncControlRepository.GetByIdAsync(syncControlId);
        if (syncControl == null)
        {
            _logger.LogError(
                "❌ [BATCH_SYNC] No se encontró registro en CO_EVENTOSCARGASINCCONTROL. SyncControlId: {SyncControlId}, ProcessType: {ProcessType}, IdCarga: {IdCarga}, ProcessId: {ProcessId}",
                syncControlId, processType, idCarga, processId);
            Console.WriteLine($"❌ [BATCH_SYNC] No se encontró registro en CO_EVENTOSCARGASINCCONTROL con SyncControlId: {syncControlId}");
            Console.Out.Flush();
            
            // Intentar obtener el lock y liberarlo si existe
            if (_activeLocks.TryRemove(processType, out var lockToDispose))
            {
                lockToDispose?.Dispose();
            }
            
            throw new InvalidOperationException($"No se encontró registro en CO_EVENTOSCARGASINCCONTROL con SyncControlId: {syncControlId}");
        }

        // Obtener el lock del diccionario estático
        if (!_activeLocks.TryRemove(processType, out var lockWrapper))
        {
            _logger.LogError(
                "❌ [BATCH_SYNC] No se encontró lock para processType: {ProcessType}, ProcessId: {ProcessId}, SyncControlId: {SyncControlId}",
                processType, processId, syncControlId);
            Console.WriteLine($"❌ [BATCH_SYNC] No se encontró lock para processType: {processType}, ProcessId: {processId}");
            Console.Out.Flush();
            
            // Actualizar status a FAILED
            try
            {
                await _syncControlRepository.UpdateStatusToFailedAsync(
                    syncControlId,
                    $"No se encontró lock para processType: {processType}",
                    null,
                    "SYSTEM");
            }
            catch { /* Ignorar */ }
            
            throw new InvalidOperationException($"No se encontró lock para processType: {processType}");
        }

        // Iniciar renovación periódica del lock (heartbeat) en background
        using var renewalCts = new CancellationTokenSource();
        var renewalTask = StartLockRenewalAsync(lockWrapper, renewalCts.Token);

        // Paso 13: Procesar el proceso obtenido (en este caso, solo tenemos uno)
        Console.WriteLine($"✅ [BATCH_SYNC] Paso 13: Proceso encontrado - SyncControlId: {syncControl.SyncControlId}");
        Console.Out.Flush();
        
        _logger.LogInformation(
            "✅ [BATCH_SYNC] Proceso encontrado. SyncControlId: {SyncControlId}, ProcessType: {ProcessType}, IdCarga: {IdCarga}",
            syncControl.SyncControlId, processType, idCarga);

        // Paso 14: Obtener dealers activos
        if (!syncControl.EventoCargaProcesoId.HasValue)
        {
            _logger.LogError(
                "❌ [BATCH_SYNC] EventoCargaProcesoId no está disponible. SyncControlId: {SyncControlId}, ProcessId: {ProcessId}",
                syncControl.SyncControlId, processId);
            await _syncControlRepository.UpdateStatusToFailedAsync(
                syncControl.SyncControlId,
                "EventoCargaProcesoId no está disponible",
                null,
                "SYSTEM");
            lockWrapper?.Dispose();
            return;
        }

        // Paso 14: Obtener dealers activos
        Console.WriteLine($"🔍 [BATCH_SYNC] Paso 14: Obteniendo dealers activos para EventoCargaProcesoId: {syncControl.EventoCargaProcesoId.Value}...");
        Console.Out.Flush();
        
        List<Dealer> dealersActivos;
        try
        {
            dealersActivos = await _dealerRepository.GetActiveDealersByProcessIdAsync(syncControl.EventoCargaProcesoId.Value);
            _logger.LogInformation(
                "✅ [BATCH_SYNC] Dealers activos obtenidos: {Cantidad} dealers para EventoCargaProcesoId: {EventoCargaProcesoId}",
                dealersActivos.Count, syncControl.EventoCargaProcesoId.Value);
            
            Console.WriteLine($"✅ [BATCH_SYNC] Paso 14 completado: {dealersActivos.Count} dealers activos obtenidos");
            Console.WriteLine($"✅ [BATCH_SYNC] Iniciando procesamiento de dealers...");
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.Out.Flush();
            
            // Mostrar información de los dealers obtenidos
            if (dealersActivos.Count > 0)
            {
                Console.WriteLine("");
                Console.WriteLine($"📋 [BATCH_SYNC] ════════════════════════════════════════════════════════════");
                Console.WriteLine($"📋 [BATCH_SYNC] LISTA DE DEALERS A SINCRONIZAR: {dealersActivos.Count} dealers total");
                Console.WriteLine($"📋 [BATCH_SYNC] ════════════════════════════════════════════════════════════");
                Console.Out.Flush();
                
                // Mostrar todos los dealers (o los primeros 20 si hay muchos)
                var dealersToShow = Math.Min(dealersActivos.Count, 20);
                for (int i = 0; i < dealersToShow; i++)
                {
                    var dealer = dealersActivos[i];
                    var urlWebhookShort = dealer.UrlWebhook.Length > 60 
                        ? dealer.UrlWebhook.Substring(0, 60) + "..." 
                        : dealer.UrlWebhook;
                    
                    // DealerBac ahora puede contener múltiples valores separados por comas
                    var dealerBacDisplay = dealer.DealerBac.Length > 40 
                        ? dealer.DealerBac.Substring(0, 40) + "..." 
                        : dealer.DealerBac;
                    
                    Console.WriteLine($"📋 [BATCH_SYNC] [{i + 1,3}/{dealersActivos.Count}] DealerBAC: {dealerBacDisplay,-43} | Estado: {dealer.EstadoWebhook,-12} | URL: {urlWebhookShort}");
                    Console.Out.Flush();
                }
                
                if (dealersActivos.Count > 20)
                {
                    Console.WriteLine($"📋 [BATCH_SYNC] ... y {dealersActivos.Count - 20} dealers más (no mostrados en detalle)");
                    Console.Out.Flush();
                }
                
                Console.WriteLine($"📋 [BATCH_SYNC] ════════════════════════════════════════════════════════════");
                Console.WriteLine("");
                Console.Out.Flush();
                
                // Log detallado en Serilog de todos los dealers
                _logger.LogInformation(
                    "📋 [BATCH_SYNC] Lista completa de dealers a sincronizar ({TotalDealers} total) para EventoCargaProcesoId: {EventoCargaProcesoId}",
                    dealersActivos.Count, syncControl.EventoCargaProcesoId.Value);
                
                foreach (var dealer in dealersActivos.Take(50)) // Log de hasta 50 dealers en Serilog
                {
                    _logger.LogDebug(
                        "📋 [BATCH_SYNC] Dealer: DealerBAC={DealerBac}, URLWebhook={UrlWebhook}, EstadoWebhook={EstadoWebhook}",
                        dealer.DealerBac, dealer.UrlWebhook, dealer.EstadoWebhook);
                }
            }
            else
            {
                Console.WriteLine($"⚠️ [BATCH_SYNC] No se encontraron dealers activos para procesar");
                Console.Out.Flush();
                _logger.LogWarning(
                    "⚠️ [BATCH_SYNC] No se encontraron dealers activos para EventoCargaProcesoId: {EventoCargaProcesoId}",
                    syncControl.EventoCargaProcesoId.Value);
            }

            // Actualizar WebhooksTotales en el registro (usando la cantidad de webhooks únicos por UrlWebhook)
            if (dealersActivos != null && dealersActivos.Any())
            {
                var webhooksUnicos = dealersActivos.Select(d => d.UrlWebhook).Distinct().Count();
                if (syncControl.WebhooksTotales != webhooksUnicos)
                {
                    syncControl.WebhooksTotales = webhooksUnicos;
                    await _syncControlRepository.UpdateAsync(syncControl, "SYSTEM");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ [BATCH_SYNC] Error al obtener dealers activos. EventoCargaProcesoId: {EventoCargaProcesoId}, ProcessId: {ProcessId}",
                syncControl.EventoCargaProcesoId.Value, processId);
            await _syncControlRepository.UpdateStatusToFailedAsync(
                syncControl.SyncControlId,
                $"Error al obtener dealers activos: {ex.Message}",
                ex.ToString(),
                "SYSTEM");
            lockWrapper?.Dispose();
            return;
        }

        try
        {
            // Ejecutar el proceso con el lock obtenido
            if (dealersActivos != null && dealersActivos.Any())
            {
                await ProcessDealersWebhooksAsync(
                    processId,
                    processType,
                    idCarga,
                    syncControl.SyncControlId,
                    syncControl.EventoCargaProcesoId.Value,
                    dealersActivos,
                    lockWrapper);
            }
            else
            {
                _logger.LogWarning(
                    "⚠️ [BATCH_SYNC] No hay dealers activos para procesar. Actualizando estado a COMPLETED sin procesar webhooks.");
                
                // Actualizar estado a COMPLETED con ceros
                await _syncControlRepository.UpdateStatusToCompletedAsync(
                    syncControl.SyncControlId,
                    0, // webhooksProcesados
                    0, // webhooksFallidos
                    0, // webhooksOmitidos
                    "SYSTEM");
            }
        }
        finally
        {
            // Detener la renovación periódica del heartbeat ANTES de liberar el lock
            renewalCts.Cancel();
            try
            {
                await renewalTask;
                _logger.LogDebug("🔄 [BATCH_SYNC] Heartbeat detenido exitosamente");
            }
            catch (OperationCanceledException)
            {
                // Esperado cuando cancelamos - no es un error
                _logger.LogDebug("🔄 [BATCH_SYNC] Heartbeat cancelado (esperado al finalizar proceso)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ [BATCH_SYNC] Error al detener heartbeat (no crítico)");
            }
            
            // Liberar el lock de Redis al finalizar (GARANTIZADO en finally)
            try
            {
                Console.WriteLine("🔓 [REDIS_LOCK] Finalizando proceso: liberando lock de Redis...");
                Console.Out.Flush();
                
                lockWrapper?.Dispose();
                
                Console.WriteLine("✅ [REDIS_LOCK] Lock de Redis liberado exitosamente - Proceso completado");
                Console.Out.Flush();
                
                _logger.LogInformation(
                    "✅ [BATCH_SYNC] Lock de Redis liberado exitosamente | ProcessType: {ProcessType}, ProcessId: {ProcessId}",
                    processType, processId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [REDIS_LOCK] Error crítico al liberar lock: {ex.Message}");
                Console.Out.Flush();
                
                _logger.LogError(ex,
                    "❌ [BATCH_SYNC] Error crítico al liberar lock de Redis | ProcessType: {ProcessType}, ProcessId: {ProcessId}",
                    processType, processId);
            }
        }
    }
    
    /// <summary>
    /// Procesa los webhooks de los dealers agrupados por UrlWebhook
    /// </summary>
    private async Task ProcessDealersWebhooksAsync(
        string processId,
        string processType,
        string idCarga,
        int syncControlId,
        int eventoCargaProcesoId,
        List<Dealer> dealersAgrupados,
        IRedisLockWrapper lockWrapper)
    {
        var webhooksProcesados = 0;
        var webhooksOmitidos = 0;
        var webhooksFallidos = 0;
        
        // Contadores de dealers
        var dealersSincronizados = 0;
        var dealersConError = 0;

        Console.WriteLine("");
        Console.WriteLine($"════════════════════════════════════════════════════════════");
        Console.WriteLine($"🔄 [BATCH_SYNC] Iniciando procesamiento de {dealersAgrupados.Count} webhooks...");
        Console.WriteLine($"════════════════════════════════════════════════════════════");
        Console.Out.Flush();

        var webhookNumero = 0;
        foreach (var dealerGrupo in dealersAgrupados)
        {
            webhookNumero++;
            List<EventoCargaSnapshotDealer>? dealersIndividuales = null;
            
            try
            {
                Console.WriteLine("");
                Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.WriteLine($"🌐 [WEBHOOK] Webhook {webhookNumero}/{dealersAgrupados.Count}: Procesando webhook");
                Console.WriteLine($"   URL: {dealerGrupo.UrlWebhook}");
                Console.WriteLine($"   DealerBACs: {dealerGrupo.DealerBac}");
                Console.WriteLine($"   Estado actual: {dealerGrupo.EstadoWebhook ?? "N/A"}");
                Console.Out.Flush();

                // Obtener todos los dealers individuales de este webhook
                Console.WriteLine($"   📋 Obteniendo dealers individuales del webhook...");
                Console.Out.Flush();
                
                dealersIndividuales = await _eventoCargaSnapshotDealerRepository
                    .GetDealersByUrlWebhookAsync(dealerGrupo.UrlWebhook, eventoCargaProcesoId);

                if (!dealersIndividuales.Any())
                {
                    Console.WriteLine($"   ⚠️  No se encontraron dealers individuales para este webhook");
                    Console.Out.Flush();
                    _logger.LogWarning(
                        "⚠️ [BATCH_SYNC] No se encontraron dealers individuales para UrlWebhook: {UrlWebhook}",
                        dealerGrupo.UrlWebhook);
                    continue;
                }

                Console.WriteLine($"   ✅ Se encontraron {dealersIndividuales.Count} dealers individuales:");
                foreach (var dealer in dealersIndividuales)
                {
                    Console.WriteLine($"      • DealerBAC: {dealer.DealerBac} | Nombre: {dealer.NombreDealer} | DMS: {dealer.Dms}");
                }
                Console.Out.Flush();

                // Construir payload del webhook
                Console.WriteLine($"   📦 Construyendo payload del webhook...");
                Console.Out.Flush();
                
                var webhookPayload = new
                {
                    processType = processType,
                    idCarga = idCarga,
                    fechaCarga = DateTimeHelper.GetMexicoDateTime(),
                    dealers = dealersIndividuales.Select(d => new
                    {
                        dealerBac = d.DealerBac,
                        nombreDealer = d.NombreDealer,
                        dms = d.Dms
                    }).ToList()
                };

                // Enviar webhook
                Console.WriteLine($"   🚀 Enviando webhook POST a: {dealerGrupo.UrlWebhook}...");
                Console.Out.Flush();
                
                var webhookResult = await _webhookSyncService.SendWebhookAsync(
                    dealerGrupo.UrlWebhook,
                    dealerGrupo.SecretKey,
                    webhookPayload);

                if (webhookResult.IsSuccess && !string.IsNullOrWhiteSpace(webhookResult.AckToken))
                {
                    // ÉXITO: Guardar registros y actualizar estado
                    Console.WriteLine($"   ✅ [WEBHOOK] Respuesta recibida: StatusCode {webhookResult.StatusCode} - Sincronización EXITOSA");
                    Console.WriteLine($"   🎫 ACK Token: {webhookResult.AckToken}");
                    Console.WriteLine($"   💾 Guardando registros en base de datos...");
                    Console.Out.Flush();

                    // Obtener conteo de registros sincronizados antes de insertar
                    var registrosSincronizadosBase = await _sincCargaProcesoDealerRepository
                        .GetCountByEventoCargaProcesoIdAsync(eventoCargaProcesoId);

                    Console.WriteLine($"      📊 Registros sincronizados base: {registrosSincronizadosBase}");
                    Console.Out.Flush();

                    // Insertar registro por cada dealer individual
                    var contador = 0;
                    foreach (var dealer in dealersIndividuales)
                    {
                        contador++;
                        Console.WriteLine($"      💾 [{contador}/{dealersIndividuales.Count}] Insertando registro para DealerBAC: {dealer.DealerBac}...");
                        Console.Out.Flush();
                        
                        var sincRegistro = new SincCargaProcesoDealer
                        {
                            EventoCargaProcesoId = eventoCargaProcesoId,
                            DmsOrigen = dealer.Dms,
                            DealerBac = dealer.DealerBac,
                            NombreDealer = dealer.NombreDealer,
                            FechaSincronizacion = DateTimeHelper.GetMexicoDateTime(),
                            RegistrosSincronizados = registrosSincronizadosBase + contador,
                            TokenConfirmacion = webhookResult.AckToken
                        };

                        await _sincCargaProcesoDealerRepository.CreateAsync(sincRegistro, "SYSTEM");
                        Console.WriteLine($"         ✅ Registro creado exitosamente (ID: {sincRegistro.SincCargaProcesoDealerId})");
                        Console.Out.Flush();
                    }

                    // Actualizar estado de dealers a EXITOSO
                    Console.WriteLine($"   🔄 Actualizando estado de {dealersIndividuales.Count} dealers a EXITOSO...");
                    Console.Out.Flush();
                    
                    await _eventoCargaSnapshotDealerRepository.UpdateWebhookStatusToExitosoAsync(
                        dealerGrupo.UrlWebhook,
                        eventoCargaProcesoId,
                        webhookResult.AckToken,
                        "SYSTEM");

                    Console.WriteLine($"      ✅ Estado actualizado a EXITOSO");
                    Console.WriteLine($"   ✅ Webhook {webhookNumero} completado exitosamente: {dealersIndividuales.Count} dealers sincronizados");
                    Console.Out.Flush();

                    webhooksProcesados++;
                    dealersSincronizados += dealersIndividuales.Count;

                    _logger.LogInformation(
                        "✅ [BATCH_SYNC] Webhook procesado exitosamente - UrlWebhook: {UrlWebhook}, Dealers: {Count}",
                        dealerGrupo.UrlWebhook, dealersIndividuales.Count);
                }
                else
                {
                    // FALLO: Actualizar estado de dealers a FALLIDO
                    var errorMessage = webhookResult.ErrorMessage ?? "Error desconocido";
                    
                    Console.WriteLine($"   ❌ [WEBHOOK] Sincronización FALLIDA");
                    Console.WriteLine($"      StatusCode: {webhookResult.StatusCode}");
                    Console.WriteLine($"      Error: {errorMessage}");
                    if (webhookResult.IsAuthError)
                    {
                        Console.WriteLine($"      Tipo: Error de Autenticación (401/403)");
                    }
                    else if (webhookResult.IsConnectionError)
                    {
                        Console.WriteLine($"      Tipo: Error de Conexión");
                    }
                    Console.Out.Flush();

                    Console.WriteLine($"   🔄 Actualizando estado de {dealersIndividuales.Count} dealers a FALLIDO...");
                    Console.Out.Flush();
                    
                    await _eventoCargaSnapshotDealerRepository.UpdateWebhookStatusToFallidoAsync(
                        dealerGrupo.UrlWebhook,
                        eventoCargaProcesoId,
                        errorMessage,
                        "SYSTEM");

                    Console.WriteLine($"      ✅ Estado actualizado a FALLIDO");
                    Console.WriteLine($"   ❌ Webhook {webhookNumero} falló: {dealersIndividuales.Count} dealers marcados como FALLIDO");
                    Console.Out.Flush();

                    webhooksFallidos++;
                    dealersConError += dealersIndividuales.Count;

                    _logger.LogWarning(
                        "⚠️ [BATCH_SYNC] Webhook procesado con error - UrlWebhook: {UrlWebhook}, Error: {ErrorMessage}, Dealers: {Count}",
                        dealerGrupo.UrlWebhook, errorMessage, dealersIndividuales.Count);
                }
                
                Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.Out.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ [ERROR] Excepción inesperada al procesar webhook:");
                Console.WriteLine($"      Tipo: {ex.GetType().Name}");
                Console.WriteLine($"      Mensaje: {ex.Message}");
                var stackTracePreview = ex.StackTrace != null && ex.StackTrace.Length > 200 
                    ? ex.StackTrace.Substring(0, 200) + "..." 
                    : ex.StackTrace ?? "N/A";
                Console.WriteLine($"      StackTrace: {stackTracePreview}");
                Console.Out.Flush();
                
                _logger.LogError(ex,
                    "❌ [BATCH_SYNC] Error al procesar webhook - UrlWebhook: {UrlWebhook}",
                    dealerGrupo.UrlWebhook);

                // Actualizar estado a FALLIDO en caso de excepción
                try
                {
                    Console.WriteLine($"   🔄 Intentando actualizar estado a FALLIDO...");
                    Console.Out.Flush();
                    
                    await _eventoCargaSnapshotDealerRepository.UpdateWebhookStatusToFallidoAsync(
                        dealerGrupo.UrlWebhook,
                        eventoCargaProcesoId,
                        $"Error inesperado: {ex.Message}",
                        "SYSTEM");
                    
                    Console.WriteLine($"      ✅ Estado actualizado a FALLIDO");
                    Console.Out.Flush();
                }
                catch (Exception updateEx)
                {
                    Console.WriteLine($"      ❌ Error al actualizar estado: {updateEx.Message}");
                    Console.Out.Flush();
                    
                    _logger.LogError(updateEx,
                        "❌ [BATCH_SYNC] Error al actualizar estado a FALLIDO - UrlWebhook: {UrlWebhook}",
                        dealerGrupo.UrlWebhook);
                }

                Console.WriteLine($"   ❌ Webhook {webhookNumero} falló por excepción");
                
                // Contar dealers con error si se pudieron obtener antes del error
                if (dealersIndividuales != null && dealersIndividuales.Any())
                {
                    dealersConError += dealersIndividuales.Count;
                    Console.WriteLine($"   ⚠️  {dealersIndividuales.Count} dealers marcados como con error");
                }
                
                Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                Console.Out.Flush();

                webhooksFallidos++;
            }
        }

        // Calcular webhooks omitidos (total - procesados - fallidos)
        webhooksOmitidos = Math.Max(0, dealersAgrupados.Count - webhooksProcesados - webhooksFallidos);

        // Obtener estadísticas de dealers
        var totalDealersCount = await _sincCargaProcesoDealerRepository
            .GetTotalDealersCountAsync(eventoCargaProcesoId);
        
        // Obtener dealers que ya estaban sincronizados (omitidos)
        var dealersSincronizadosBD = await _sincCargaProcesoDealerRepository
            .GetCountByEventoCargaProcesoIdAsync(eventoCargaProcesoId);
        
        // Calcular dealers omitidos (total - sincronizados en esta ejecución - con error)
        var dealersOmitidos = Math.Max(0, totalDealersCount - dealersSincronizados - dealersConError);

        // Actualizar SyncControl con resultados
        Console.WriteLine("");
        Console.WriteLine($"════════════════════════════════════════════════════════════");
        Console.WriteLine($"📊 [BATCH_SYNC] RESUMEN FINAL");
        Console.WriteLine($"════════════════════════════════════════════════════════════");
        Console.WriteLine($"");
        Console.WriteLine($"🌐 WEBHOOKS:");
        Console.WriteLine($"   📦 Total de webhooks procesados: {dealersAgrupados.Count}");
        Console.WriteLine($"   ✅ Total de webhooks exitosos: {webhooksProcesados}");
        Console.WriteLine($"   ❌ Total de webhooks con error: {webhooksFallidos}");
        Console.WriteLine($"   ⏭️  Total de webhooks omitidos: {webhooksOmitidos}");
        Console.WriteLine($"");
        Console.WriteLine($"👥 DEALERS:");
        Console.WriteLine($"   📦 Total de dealers: {totalDealersCount}");
        Console.WriteLine($"   ✅ Dealers sincronizados: {dealersSincronizados}");
        Console.WriteLine($"   ❌ Dealers con error: {dealersConError}");
        Console.WriteLine($"   ⏭️  Dealers omitidos (ya sincronizados): {dealersOmitidos}");
        Console.WriteLine($"════════════════════════════════════════════════════════════");
        Console.Out.Flush();
        
        try
        {
            Console.WriteLine($"   🔄 Actualizando estado del proceso a COMPLETED...");
            Console.Out.Flush();
            
            await _syncControlRepository.UpdateStatusToCompletedAsync(
                syncControlId,
                webhooksProcesados,
                webhooksFallidos,
                webhooksOmitidos,
                "SYSTEM");
            
            Console.WriteLine($"      ✅ Estado actualizado a COMPLETED (SyncControlId: {syncControlId})");
            Console.WriteLine($"         Webhooks procesados: {webhooksProcesados}, Fallidos: {webhooksFallidos}, Omitidos: {webhooksOmitidos}");
            Console.Out.Flush();

            // Actualizar CO_EVENTOSCARGAPROCESO con dealers sincronizados y porcentaje
            try
            {
                Console.WriteLine($"   🔄 Actualizando CO_EVENTOSCARGAPROCESO con dealers sincronizados...");
                Console.Out.Flush();

                // Usar el conteo ya obtenido anteriormente (dealersSincronizadosBD)
                // Calcular porcentaje
                var porcentajeSincronizados = totalDealersCount > 0
                    ? Math.Round((decimal)dealersSincronizadosBD * 100 / totalDealersCount, 2)
                    : 0;

                Console.WriteLine($"      📊 Dealers sincronizados: {dealersSincronizadosBD} de {totalDealersCount} ({porcentajeSincronizados}%)");
                Console.Out.Flush();

                await _dealerRepository.UpdateDealersSincronizadosAsync(
                    eventoCargaProcesoId,
                    dealersSincronizadosBD,
                    porcentajeSincronizados,
                    "SYSTEM");

                Console.WriteLine($"      ✅ CO_EVENTOSCARGAPROCESO actualizado exitosamente");
                Console.Out.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ Error al actualizar CO_EVENTOSCARGAPROCESO: {ex.Message}");
                Console.Out.Flush();

                _logger.LogError(ex,
                    "❌ [BATCH_SYNC] Error al actualizar CO_EVENTOSCARGAPROCESO - EventoCargaProcesoId: {EventoCargaProcesoId}",
                    eventoCargaProcesoId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"      ❌ Error al actualizar estado: {ex.Message}");
            Console.Out.Flush();
            
            _logger.LogError(ex,
                "❌ [BATCH_SYNC] Error al actualizar SyncControl a COMPLETED - SyncControlId: {SyncControlId}",
                syncControlId);
        }

        Console.WriteLine($"════════════════════════════════════════════════════════════");
        Console.WriteLine($"✅ [BATCH_SYNC] PROCESAMIENTO COMPLETADO");
        Console.WriteLine($"════════════════════════════════════════════════════════════");
        Console.WriteLine("");
        Console.Out.Flush();

        _logger.LogInformation(
            "✅ [BATCH_SYNC] Procesamiento de webhooks completado - ProcessId: {ProcessId}, Webhooks procesados: {WebhooksProcesados}, Webhooks fallidos: {WebhooksFallidos}, Webhooks omitidos: {WebhooksOmitidos}",
            processId, webhooksProcesados, webhooksFallidos, webhooksOmitidos);
    }

    /// <summary>
    /// Registra un lock activo para que Hangfire pueda acceder a él
    /// </summary>
    public static void RegisterActiveLock(string processType, IRedisLockWrapper lockWrapper)
    {
        _activeLocks.TryAdd(processType, lockWrapper);
    }

    /// <summary>
    /// Renueva periódicamente el lock mientras el proceso está activo (heartbeat)
    /// </summary>
    private async Task StartLockRenewalAsync(IRedisLockWrapper lockWrapper, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(LOCK_RENEWAL_INTERVAL_SECONDS), cancellationToken);
                
                if (cancellationToken.IsCancellationRequested)
                    break;

                var renewed = await _distributedLockService.RenewLockAsync(
                    lockWrapper.ProcessType,
                    lockWrapper.LockValue,
                    TimeSpan.FromSeconds(LOCK_RENEWAL_EXPIRY_SECONDS));

                if (renewed)
                {
                    // Log en consola para ver el heartbeat funcionando
                    Console.WriteLine($"🔄 [REDIS_LOCK] Lock renovado exitosamente (heartbeat) | ProcessType: {lockWrapper.ProcessType}");
                    Console.Out.Flush();
                    
                    _logger.LogDebug(
                        "🔄 [BATCH_SYNC] Lock renovado exitosamente | ProcessType: {ProcessType}, LockValue: {LockValue}, NuevoExpiry: {ExpirySeconds}s",
                        lockWrapper.ProcessType, lockWrapper.LockValue, LOCK_RENEWAL_EXPIRY_SECONDS);
                }
                else
                {
                    _logger.LogWarning(
                        "⚠️ [BATCH_SYNC] No se pudo renovar el lock | ProcessType: {ProcessType}, LockValue: {LockValue}",
                        lockWrapper.ProcessType, lockWrapper.LockValue);
                    break; // Si no se puede renovar, salir del loop
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelación normal, no es un error
            _logger.LogDebug(
                "🔄 [BATCH_SYNC] Renovación de lock cancelada | ProcessType: {ProcessType}",
                lockWrapper.ProcessType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ [BATCH_SYNC] Error en la renovación periódica del lock | ProcessType: {ProcessType}",
                lockWrapper.ProcessType);
        }
    }
}

