using GM.DealerSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace GM.DealerSync.Application.Services;

/// <summary>
/// Implementación del servicio de distributed locking usando Redis directo (sin RedLock)
/// </summary>
public class DistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<DistributedLockService> _logger;
    private const int DEFAULT_EXPIRY_SECONDS = 60; // 1 minuto por defecto

    public DistributedLockService(
        IConnectionMultiplexer? redis,
        ILogger<DistributedLockService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IDisposable?> TryAcquireLockAsync(string processType, TimeSpan? expiryTime = null)
    {
        if (string.IsNullOrWhiteSpace(processType))
        {
            throw new ArgumentException("El processType no puede estar vacío", nameof(processType));
        }

        if (_redis == null || !_redis.IsConnected)
        {
            _logger.LogError(
                "❌ [DISTRIBUTED_LOCK] Redis no está disponible. Redis no está configurado o no está conectado.");
            throw new InvalidOperationException("Redis no está disponible. Redis no está configurado o no está conectado. " +
                                              "Por favor, asegúrese de que Redis esté corriendo y configurado correctamente en appsettings.json.");
        }

        var lockKey = $"lock:sync:{processType}";
        var lockValue = Guid.NewGuid().ToString(); // Valor único para identificar este lock
        var expiry = expiryTime ?? TimeSpan.FromSeconds(DEFAULT_EXPIRY_SECONDS);

        try
        {
            _logger.LogInformation(
                "🔒 [DISTRIBUTED_LOCK] Intentando adquirir lock para processType: {ProcessType}, Key: {LockKey}, Expiry: {ExpirySeconds}s",
                processType, lockKey, expiry.TotalSeconds);

            var db = _redis.GetDatabase();
            
            // Usar SET con NX (Not exists) y EX (Expiry) - solo establece si no existe
            // Esto es atómico y seguro para distributed locking
            var lockAcquired = await db.StringSetAsync(
                key: lockKey,
                value: lockValue,
                expiry: expiry,
                when: When.NotExists); // Solo si NO existe (NX)

            if (lockAcquired)
            {
                // Log detallado solo en Serilog
                _logger.LogInformation(
                    "✅ [DISTRIBUTED_LOCK] Lock adquirido exitosamente | ProcessType: {ProcessType}, Key: {LockKey}, Value: {LockValue}, Expiry: {ExpirySeconds}s",
                    processType, lockKey, lockValue, expiry.TotalSeconds);

                // Retornar un wrapper que libera el lock al hacer Dispose
                return new RedisLockWrapper(_redis, processType, lockKey, lockValue, _logger);
            }
            else
            {
                // Log detallado solo en Serilog
                _logger.LogWarning(
                    "⚠️ [DISTRIBUTED_LOCK] Lock DENEGADO | ProcessType: {ProcessType}, Key: {LockKey}",
                    processType, lockKey);
                
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ [DISTRIBUTED_LOCK] Error al intentar adquirir lock para processType: {ProcessType}, Key: {LockKey}",
                processType, lockKey);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsLockActiveAsync(string processType)
    {
        if (string.IsNullOrWhiteSpace(processType))
        {
            return false;
        }

        if (_redis == null || !_redis.IsConnected)
        {
            _logger.LogWarning(
                "⚠️ [DISTRIBUTED_LOCK] Redis no está disponible. No se puede verificar el estado del lock.");
            return false;
        }

        var lockKey = $"lock:sync:{processType}";

        try
        {
            var db = _redis.GetDatabase();
            
            // Verificar si la key existe en Redis
            var exists = await db.KeyExistsAsync(lockKey);
            
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ [DISTRIBUTED_LOCK] Error al verificar lock activo para processType: {ProcessType}",
                processType);
            // En caso de error, asumimos que el lock está activo para ser conservadores
            return true;
        }
    }

    /// <summary>
    /// Wrapper para manejar el dispose del lock de Redis
    /// </summary>
    private class RedisLockWrapper : IDisposable
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly string _processType;
        private readonly string _lockKey;
        private readonly string _lockValue;
        private readonly ILogger<DistributedLockService> _logger;
        private bool _disposed = false;

        public RedisLockWrapper(
            IConnectionMultiplexer redis,
            string processType,
            string lockKey,
            string lockValue,
            ILogger<DistributedLockService> logger)
        {
            _redis = redis;
            _processType = processType;
            _lockKey = lockKey;
            _lockValue = lockValue;
            _logger = logger;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    // Eliminar el lock de Redis solo si el valor coincide (para evitar liberar locks de otros procesos)
                    var db = _redis.GetDatabase();
                    var currentValue = db.StringGet(_lockKey);
                    
                    if (currentValue == _lockValue)
                    {
                        db.KeyDelete(_lockKey);
                        
                        // Log simplificado en consola
                        Console.WriteLine("🔓 [REDIS_LOCK] Lock eliminado exitosamente de Redis");
                        Console.Out.Flush();
                        
                        // Log detallado solo en Serilog
                        _logger.LogInformation(
                            "🔓 [DISTRIBUTED_LOCK] Lock eliminado exitosamente de Redis | ProcessType: {ProcessType}, Key: {LockKey}",
                            _processType, _lockKey);
                    }
                    else
                    {
                        // Log detallado solo en Serilog (el lock expiró es normal, no es necesario mostrar en consola)
                        _logger.LogWarning(
                            "⚠️ [DISTRIBUTED_LOCK] Lock ya expiró o fue reemplazado | ProcessType: {ProcessType}, Key: {LockKey}",
                            _processType, _lockKey);
                    }
                }
                catch (Exception ex)
                {
                    // Log de error en consola solo si es crítico
                    Console.WriteLine($"❌ [REDIS_LOCK] Error al liberar lock: {ex.Message}");
                    Console.Out.Flush();
                    
                    // Log detallado solo en Serilog
                    _logger.LogError(ex,
                        "❌ [DISTRIBUTED_LOCK] Error al liberar lock para processType: {ProcessType}, Key: {LockKey}",
                        _processType, _lockKey);
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}

