using Microsoft.Extensions.Logging;
using Shared.Security;
using System.Collections.Concurrent;

namespace GM.DealerSync.Application.Services;

/// <summary>
/// Implementación del servicio de jobs de sincronización batch
/// </summary>
public class BatchSyncJobService : IBatchSyncJobService
{
    private readonly ILogger<BatchSyncJobService> _logger;
    private const int SIMULATED_PROCESSING_SECONDS = 60; // 1 minuto de simulación
    private const int LOG_INTERVAL_SECONDS = 5; // Log cada 5 segundos
    
    // Almacenar locks por processType para que Hangfire pueda acceder a ellos
    private static readonly ConcurrentDictionary<string, IDisposable> _activeLocks = new();

    public BatchSyncJobService(ILogger<BatchSyncJobService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ExecuteBatchSyncAsync(string processId, string processType, string idCarga, IDisposable lockDisposable)
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
                Console.Out.Flush();
                
                // Log detallado solo en Serilog (no duplicar en consola)
                _logger.LogInformation(
                    "🚀 [BATCH_SYNC] INICIANDO proceso de sincronización batch | ProcessId: {ProcessId}, ProcessType: {ProcessType}, IdCarga: {IdCarga}, StartTime: {StartTime}",
                    processId, processType, idCarga, startTimeString);

            // Dividir el sleep en intervalos de 5 segundos para loguear progreso
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

                var endTime = DateTimeHelper.GetMexicoDateTime();
                var endTimeString = DateTimeHelper.GetMexicoTimeString();
                var duration = (endTime - startTime).TotalSeconds;
                var durationMinutes = Math.Round(duration / 60.0, 2);

                // Log simplificado de finalización en consola
                Console.WriteLine("✅ [BATCH_SYNC] PROCESO FINALIZADO");
                Console.Out.Flush();
                
                // Log detallado solo en Serilog
                _logger.LogInformation(
                    "✅ [BATCH_SYNC] PROCESO FINALIZADO | ProcessId: {ProcessId}, ProcessType: {ProcessType}, IdCarga: {IdCarga}, StartTime: {StartTime}, EndTime: {EndTime}, Duración: {DurationSeconds}s ({DurationMinutes} min)",
                    processId, processType, idCarga, startTimeString, endTimeString, duration, durationMinutes);
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
                
                // Liberar el lock al finalizar (esto mostrará logs del RedLockWrapper)
                lockDisposable?.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public async Task ExecuteBatchSyncWithHangfireAsync(string processId, string processType, string idCarga)
    {
        // Obtener el lock del diccionario estático
        if (!_activeLocks.TryRemove(processType, out var lockDisposable))
        {
            _logger.LogError(
                "❌ [BATCH_SYNC] No se encontró lock para processType: {ProcessType}, ProcessId: {ProcessId}",
                processType, processId);
            Console.WriteLine($"❌ [BATCH_SYNC] No se encontró lock para processType: {processType}, ProcessId: {processId}");
            throw new InvalidOperationException($"No se encontró lock para processType: {processType}");
        }

        // Ejecutar el proceso con el lock obtenido
        await ExecuteBatchSyncAsync(processId, processType, idCarga, lockDisposable);
    }
    
    /// <summary>
    /// Registra un lock activo para que Hangfire pueda acceder a él
    /// </summary>
    public static void RegisterActiveLock(string processType, IDisposable lockDisposable)
    {
        _activeLocks.TryAdd(processType, lockDisposable);
    }
}

