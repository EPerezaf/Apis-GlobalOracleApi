using GM.DealerSync.Application.DTOs;
using GM.DealerSync.Application.Services;
using GM.DealerSync.Domain.Entities;
using GM.DealerSync.Domain.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Responses;
using Shared.Security;
using StackExchange.Redis;

namespace GM.DealerSync.API.Controllers;

/// <summary>
/// Controller para sincronización batch de procesos
/// </summary>
[ApiController]
[Route("api/v1/gm/dealer-sync/batch-sincronizacion-procesos")]
[Produces("application/json")]
[Authorize]
public class BatchSincronizacionProcesosController : ControllerBase
{
    private readonly IDistributedLockService? _distributedLockService;
    private readonly IBatchSyncJobService _batchSyncJobService;
    private readonly IProcessTypeService _processTypeService;
    private readonly ISyncControlRepository _syncControlRepository;
    private readonly IDealerRepository _dealerRepository;
    private readonly ILogger<BatchSincronizacionProcesosController> _logger;
    private readonly IConnectionMultiplexer? _redisConnection;
    private const int LOCK_INITIAL_EXPIRY_SECONDS = 600; // 10 minutos iniciales (se renovará dinámicamente)

    public BatchSincronizacionProcesosController(
        IDistributedLockService? distributedLockService,
        IBatchSyncJobService batchSyncJobService,
        IProcessTypeService processTypeService,
        ISyncControlRepository syncControlRepository,
        IDealerRepository dealerRepository,
        ILogger<BatchSincronizacionProcesosController> logger,
        IConnectionMultiplexer? redisConnection = null)
    {
        _distributedLockService = distributedLockService;
        _batchSyncJobService = batchSyncJobService;
        _processTypeService = processTypeService;
        _syncControlRepository = syncControlRepository;
        _dealerRepository = dealerRepository;
        _logger = logger;
        _redisConnection = redisConnection;
    }

    /// <summary>
    /// Inicia un proceso de sincronización batch adquiriendo un lock y ejecutando el proceso automáticamente
    /// </summary>
    /// <remarks>
    /// Este endpoint adquiere el distributed lock Y ejecuta el proceso automáticamente.
    /// 
    /// **Flujo del proceso:**
    /// 1. Valida que el processType esté en la lista de procesos permitidos e implementados
    /// 2. Intenta adquirir un distributed lock para el processType especificado
    /// 3. Si el lock ya existe (proceso en ejecución), retorna 409 Conflict
    /// 4. Si el lock se adquiere exitosamente, encola el proceso en Hangfire (background)
    /// 5. El proceso se ejecuta completamente en background usando Hangfire, procesando todos los dealers
    /// 6. Retorna 202 Accepted con ProcessId inmediatamente (el proceso continúa en background)
    /// 7. Al finalizar el proceso (si todo está bien), se actualiza el estado en BD a COMPLETED
    /// 8. El lock se libera automáticamente cuando el proceso finaliza
    /// 
    /// **Procesos disponibles:**
    /// - ProductList: Sincronización de lista de productos
    /// 
    /// **Nota:** Si se intenta ejecutar un proceso no implementado, se retornará 400 Bad Request con la lista de procesos disponibles.
    /// </remarks>
    /// <param name="dto">Datos del proceso de sincronización batch</param>
    /// <returns>Respuesta con el ProcessId y confirmación de inicio</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BatchSincronizacionProcesosResponseDto>), 202)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    [ProducesResponseType(typeof(ApiResponse), 409)]
    [ProducesResponseType(typeof(ApiResponse), 500)]
    [ProducesResponseType(typeof(ApiResponse), 503)]
    public async Task<IActionResult> IniciarBatchSincronizacionProcesos(
        [FromBody] BatchSincronizacionProcesosDto dto)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);
        var startTime = DateTimeHelper.GetMexicoDateTime();
        
        // Generar ProcessId único
        var processId = Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper();

        _logger.LogInformation(
            "📥 [BATCH_SYNC] Solicitud recibida. ProcessType: {ProcessType}, IdCarga: {IdCarga}, CorrelationId: {CorrelationId}, ProcessId: {ProcessId}",
            dto.ProcessType, dto.IdCarga, correlationId, processId);

        try
        {
            // Validar modelo
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning(
                    "⚠️ [BATCH_SYNC] Validación fallida. ProcessType: {ProcessType}, IdCarga: {IdCarga}, Errors: {Errors}, ProcessId: {ProcessId}",
                    dto.ProcessType, dto.IdCarga, string.Join(", ", errors), processId);

                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = $"Validación fallida: {string.Join(", ", errors)}",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            // Paso 4: Validar que el proceso esté implementado y permitido
            if (string.IsNullOrWhiteSpace(dto.ProcessType) || !_processTypeService.IsProcessTypeImplemented(dto.ProcessType))
            {
                var procesosImplementados = _processTypeService.GetImplementedProcessTypes().ToList();
                var procesosDisponibles = _processTypeService.GetAllAvailableProcessTypes().ToList();
                var procesosDisponiblesStr = string.Join(", ", procesosImplementados);
                
                var mensajeError = $"El proceso '{dto.ProcessType}' no está implementado o no está permitido para sincronización batch. " +
                                 $"Procesos implementados y disponibles: {procesosDisponiblesStr}";
                
                _logger.LogWarning(
                    "⚠️ [BATCH_SYNC] Proceso no implementado. ProcessType: {ProcessType}, IdCarga: {IdCarga}, ProcessId: {ProcessId}, ProcesosImplementados: {ProcesosImplementados}",
                    dto.ProcessType, dto.IdCarga, processId, procesosDisponiblesStr);
                
                Console.WriteLine($"⚠️ [BATCH_SYNC] PROCESO NO IMPLEMENTADO: {dto.ProcessType}");
                Console.WriteLine($"⚠️ [BATCH_SYNC] Procesos implementados: {procesosDisponiblesStr}");
                Console.Out.Flush();

                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = mensajeError,
                    Data = new
                    {
                        ProcessTypeSolicitado = dto.ProcessType,
                        ProcesosImplementados = procesosImplementados,
                        TodosLosProcesosDisponibles = procesosDisponibles,
                        Mensaje = "El proceso solicitado aún no está implementado. Solo los procesos en la lista de 'ProcesosImplementados' pueden ejecutarse."
                    },
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            // Paso 5: Intentar adquirir lock
            if (_distributedLockService == null)
            {
                _logger.LogError(
                    "❌ [BATCH_SYNC] DistributedLockService no está disponible. Redis no está configurado o no está disponible. ProcessId: {ProcessId}",
                    processId);
                return StatusCode(503, new ApiResponse
                {
                    Success = false,
                    Message = "Servicio de distributed locking no disponible. Redis no está configurado o no está disponible.",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            _logger.LogInformation(
                "🔒 [BATCH_SYNC] Intentando adquirir lock para processType: {ProcessType}, ProcessId: {ProcessId}",
                dto.ProcessType, processId);

            var expiryTime = TimeSpan.FromSeconds(LOCK_INITIAL_EXPIRY_SECONDS);
            var lockWrapper = await _distributedLockService.TryAcquireLockAsync(dto.ProcessType, expiryTime);

            // Paso 6-7: Si el lock ya existe (proceso en ejecución)
            if (lockWrapper == null)
            {
                var currentTimeString = DateTimeHelper.GetMexicoTimeString();
                
                // Log detallado solo en Serilog
                _logger.LogWarning(
                    "⚠️ [BATCH_SYNC] PROCESO OCUPADO - Lock DENEGADO | ProcessType: {ProcessType}, IdCarga: {IdCarga}, CorrelationId: {CorrelationId}, ProcessId: {ProcessId}, Timestamp: {Timestamp}, Razón: Ya existe un proceso en ejecución",
                    dto.ProcessType, dto.IdCarga, correlationId, processId, currentTimeString);

                return Conflict(new ApiResponse<BatchSincronizacionProcesosResponseDto>
                {
                    Success = false,
                    Message = $"⚠️ PROCESO OCUPADO: El processType '{dto.ProcessType}' está siendo procesado actualmente. " +
                             $"Intente nuevamente después de que finalice el proceso actual.",
                    Data = new BatchSincronizacionProcesosResponseDto
                    {
                        ProcessId = processId,
                        LockAcquired = false,
                        ProcessType = dto.ProcessType,
                        IdCarga = dto.IdCarga,
                        Message = $"Proceso ya en ejecución. El lock se renovará dinámicamente hasta que termine el proceso.",
                        StartTime = startTime,
                        LockExpirySeconds = LOCK_INITIAL_EXPIRY_SECONDS
                    },
                    Timestamp = currentTimeString
                });
            }

            // Paso 8: Lock adquirido exitosamente
            var lockAcquiredTime = DateTimeHelper.GetMexicoDateTime();
            var lockAcquiredTimeString = DateTimeHelper.GetMexicoTimeString();
            var usuarioAlta = JwtUserHelper.GetCurrentUser(User, _logger);

            // Paso 9: Obtener EventoCargaProcesoId y FechaCarga desde CO_EVENTOSCARGAPROCESO
            var eventoInfo = await _dealerRepository.GetEventoCargaProcesoInfoAsync(dto.ProcessType, dto.IdCarga);
            if (!eventoInfo.HasValue)
            {
                _logger.LogWarning(
                    "⚠️ [BATCH_SYNC] No se encontró EventoCargaProcesoId para ProcessType: {ProcessType}, IdCarga: {IdCarga}",
                    dto.ProcessType, dto.IdCarga);
                lockWrapper?.Dispose();
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = $"No se encontró un proceso de carga con ProcessType '{dto.ProcessType}' e IdCarga '{dto.IdCarga}'",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            var eventoCargaProcesoId = eventoInfo.Value.EventoCargaProcesoId;
            var fechaCarga = eventoInfo.Value.FechaCarga;

            // Validar el Estatus del proceso antes de continuar
            var estatusProceso = await _dealerRepository.GetEventoCargaProcesoEstatusAsync(dto.ProcessType, dto.IdCarga);
            if (!string.IsNullOrWhiteSpace(estatusProceso) && estatusProceso.Equals("SINCRONIZADA", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "⚠️ [BATCH_SYNC] El proceso ya está sincronizado. ProcessType: {ProcessType}, IdCarga: {IdCarga}, Estatus: {Estatus}",
                    dto.ProcessType, dto.IdCarga, estatusProceso);
                
                lockWrapper?.Dispose();
                
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = $"El proceso '{dto.ProcessType}' con IdCarga '{dto.IdCarga}' ya está sincronizado. " +
                             $"Estatus actual: {estatusProceso}. " +
                             $"No se puede ejecutar nuevamente el proceso de sincronización.",
                    Data = new
                    {
                        ProcessType = dto.ProcessType,
                        IdCarga = dto.IdCarga,
                        Estatus = estatusProceso,
                        EventoCargaProcesoId = eventoCargaProcesoId
                    },
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            // Paso 9: Validar si hay procesos en ejecución (RUNNING o PENDING) para evitar ejecuciones concurrentes
            // NOTA: No validamos COMPLETED o FAILED porque esos procesos ya terminaron y permitimos re-ejecutar
            SyncControl syncControl;
            try
            {
                var registroActivo = await _syncControlRepository.GetByProcessAsync(dto.ProcessType, dto.IdCarga, fechaCarga);
                
                if (registroActivo != null && (registroActivo.Status == "PENDING" || registroActivo.Status == "RUNNING"))
                {
                    // Hay un proceso en curso o pendiente - no permitir ejecutar otro
                    _logger.LogWarning(
                        "⚠️ [BATCH_SYNC] Ya existe un proceso en estado {Status} para ProcessType: {ProcessType}, IdCarga: {IdCarga}. SyncControlId: {SyncControlId}",
                        registroActivo.Status, dto.ProcessType, dto.IdCarga, registroActivo.SyncControlId);
                    lockWrapper?.Dispose();
                    return Conflict(new ApiResponse
                    {
                        Success = false,
                        Message = $"Ya existe un proceso en estado '{registroActivo.Status}' para ProcessType '{dto.ProcessType}' e IdCarga '{dto.IdCarga}'. " +
                                 $"Debe esperar a que termine o finalice para poder ejecutarlo nuevamente.",
                        Data = new
                        {
                            SyncControlId = registroActivo.SyncControlId,
                            Status = registroActivo.Status,
                            ProcessType = registroActivo.ProcessType,
                            IdCarga = registroActivo.IdCarga
                        },
                        Timestamp = DateTimeHelper.GetMexicoTimeString()
                    });
                }

                // Si no hay procesos activos (o todos están COMPLETED/FAILED), crear un NUEVO registro
                // Esto permite mantener historial de todas las ejecuciones
                syncControl = new SyncControl
                {
                    ProcessType = dto.ProcessType,
                    IdCarga = dto.IdCarga,
                    FechaCarga = fechaCarga, // Usar fecha del proceso obtenida desde la BD
                    EventoCargaProcesoId = eventoCargaProcesoId,
                    Status = "PENDING",
                    WebhooksTotales = 0
                };
                
                syncControl = await _syncControlRepository.CreateAsync(syncControl, usuarioAlta);
                _logger.LogInformation(
                    "✅ [BATCH_SYNC] Nuevo registro creado en CO_EVENTOSCARGASINCCONTROL. SyncControlId: {SyncControlId}",
                    syncControl.SyncControlId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ [BATCH_SYNC] Error al crear registro en CO_EVENTOSCARGASINCCONTROL. ProcessId: {ProcessId}",
                    processId);
                lockWrapper?.Dispose();
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = $"Error al registrar el proceso en la base de datos: {ex.Message}",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            // Paso 10: Encolar UN SOLO job en Hangfire que procesará TODOS los webhooks en paralelo
            // IMPORTANTE: Solo se crea UN job de Hangfire. Los webhooks se procesan en paralelo
            // DENTRO de este job usando TPL (Parallel.ForEachAsync). NO se crean jobs adicionales por cada webhook.
            string hangfireJobId;
            try
            {
                hangfireJobId = BackgroundJob.Enqueue<IBatchSyncJobService>(service =>
                    service.ExecuteBatchSyncWithHangfireAsync(syncControl.SyncControlId, processId, dto.ProcessType, dto.IdCarga));

                _logger.LogInformation(
                    "✅ [BATCH_SYNC] UN SOLO job de Hangfire encolado | HangfireJobId: {HangfireJobId}, ProcessId: {ProcessId} | Este job procesará TODOS los webhooks en paralelo dentro del mismo job usando TPL",
                    hangfireJobId, processId);

                // Registrar el lock para que Hangfire pueda acceder a él
                BatchSyncJobService.RegisterActiveLock(dto.ProcessType, lockWrapper);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ [BATCH_SYNC] Error al encolar job en Hangfire. ProcessId: {ProcessId}",
                    processId);
                lockWrapper?.Dispose();
                // Actualizar status a FAILED en la BD
                try
                {
                    await _syncControlRepository.UpdateStatusToFailedAsync(
                        syncControl.SyncControlId,
                        "Error al encolar job en Hangfire",
                        ex.ToString(),
                        usuarioAlta);
                }
                catch { /* Ignorar error de actualización */ }
                return StatusCode(500, new ApiResponse
                {
                    Success = false,
                    Message = "Error al encolar el job en Hangfire",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            // Paso 9 (continuación): Actualizar status a RUNNING con HangfireJobId
            try
            {
                await _syncControlRepository.UpdateStatusToRunningAsync(
                    syncControl.SyncControlId,
                    hangfireJobId,
                    usuarioAlta);

                _logger.LogInformation(
                    "✅ [BATCH_SYNC] Status actualizado a RUNNING. SyncControlId: {SyncControlId}, HangfireJobId: {HangfireJobId}",
                    syncControl.SyncControlId, hangfireJobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "⚠️ [BATCH_SYNC] Error al actualizar status a RUNNING. SyncControlId: {SyncControlId}",
                    syncControl.SyncControlId);
                // Continuar aunque falle la actualización
            }

            // Logs simplificados en consola
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.WriteLine($"✅ [BATCH_SYNC] PROCESO INICIADO");
            Console.WriteLine($"✅ [BATCH_SYNC] ProcessId: {processId}");
            Console.WriteLine($"✅ [BATCH_SYNC] ProcessType: {dto.ProcessType}");
            Console.WriteLine($"✅ [BATCH_SYNC] IdCarga: {dto.IdCarga}");
            Console.WriteLine($"✅ [BATCH_SYNC] SyncControlId: {syncControl.SyncControlId}");
            Console.WriteLine($"✅ [BATCH_SYNC] HangfireJobId: {hangfireJobId}");
            Console.WriteLine($"✅ [DISTRIBUTED_LOCK] Lock adquirido exitosamente");
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.Out.Flush();
            
            // Log detallado solo en Serilog
            _logger.LogInformation(
                "✅ [BATCH_SYNC] Job registrado y encolado en Hangfire | ProcessType: {ProcessType}, IdCarga: {IdCarga}, CorrelationId: {CorrelationId}, ProcessId: {ProcessId}, SyncControlId: {SyncControlId}, HangfireJobId: {HangfireJobId}, LockAcquiredTime: {LockAcquiredTime}",
                dto.ProcessType, dto.IdCarga, correlationId, processId, syncControl.SyncControlId, hangfireJobId, lockAcquiredTimeString);

            // Paso 11: Retornar 202 Accepted (Job encolado, se ejecutará en background con Hangfire)
            var response = new BatchSincronizacionProcesosResponseDto
            {
                ProcessId = processId,
                LockAcquired = true,
                ProcessType = dto.ProcessType,
                IdCarga = dto.IdCarga,
                Message = $"✅ Proceso de sincronización batch iniciado exitosamente y encolado en Hangfire. " +
                         $"ProcessId: {processId}, HangfireJobId: {hangfireJobId}. " +
                         $"El proceso se ejecutará en background y se actualizará el estado en BD al finalizar.",
                StartTime = startTime,
                LockExpirySeconds = LOCK_INITIAL_EXPIRY_SECONDS
            };

            _logger.LogInformation(
                "✅ [BATCH_SYNC] Job encolado exitosamente - Retornando 202 Accepted | ProcessId: {ProcessId}, ProcessType: {ProcessType}, IdCarga: {IdCarga}, CorrelationId: {CorrelationId}, HangfireJobId: {HangfireJobId}",
                processId, dto.ProcessType, dto.IdCarga, correlationId, hangfireJobId);

            return Accepted(new ApiResponse<BatchSincronizacionProcesosResponseDto>
            {
                Success = true,
                Message = response.Message,
                Data = response,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("RedLockFactory"))
        {
            _logger.LogError(ex,
                "❌ [BATCH_SYNC] Redis no está disponible. ProcessType: {ProcessType}, IdCarga: {IdCarga}, CorrelationId: {CorrelationId}, ProcessId: {ProcessId}",
                dto.ProcessType, dto.IdCarga, correlationId, processId);

            return StatusCode(503, new ApiResponse
            {
                Success = false,
                Message = "Servicio de distributed locking no disponible. Redis no está configurado o no está disponible. " +
                         "Por favor, asegúrese de que Redis esté corriendo y configurado correctamente.",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ [BATCH_SYNC] Error inesperado al adquirir lock. ProcessType: {ProcessType}, IdCarga: {IdCarga}, CorrelationId: {CorrelationId}, ProcessId: {ProcessId}, Error: {ErrorMessage}",
                dto.ProcessType, dto.IdCarga, correlationId, processId, ex.Message);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = $"Error interno del servidor al adquirir el lock: {ex.Message}",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }


    /// <summary>
    /// Verifica el estado del lock para un processType específico
    /// </summary>
    /// <remarks>
    /// Este endpoint permite verificar si hay un proceso en ejecución (lock activo) para un processType específico.
    /// Útil para verificar el estado antes de intentar adquirir un nuevo lock.
    /// </remarks>
    /// <param name="processType">Tipo de proceso a verificar (ej: "productList")</param>
    /// <returns>Estado del lock y procesos pendientes</returns>
    [HttpGet("estado/{processType}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 503)]
    public async Task<IActionResult> VerificarEstadoLock([FromRoute] string processType)
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);

        _logger.LogInformation(
            "🔍 [BATCH_SYNC] Verificando estado del lock. ProcessType: {ProcessType}, CorrelationId: {CorrelationId}",
            processType, correlationId);

        try
        {
            if (_distributedLockService == null)
            {
                return StatusCode(503, new ApiResponse
                {
                    Success = false,
                    Message = "Servicio de distributed locking no disponible. Redis no está configurado o no está disponible.",
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            var isLockActive = await _distributedLockService.IsLockActiveAsync(processType);

            var estado = new
            {
                ProcessType = processType,
                LockActivo = isLockActive,
                Mensaje = isLockActive
                    ? $"⚠️ El processType '{processType}' tiene un lock activo. Hay un proceso en ejecución."
                    : $"✅ El processType '{processType}' está disponible. No hay locks activos."
            };

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = estado.Mensaje,
                Data = estado,
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ [BATCH_SYNC] Error al verificar estado del lock. ProcessType: {ProcessType}, CorrelationId: {CorrelationId}",
                processType, correlationId);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = $"Error al verificar estado del lock: {ex.Message}",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }

    /// <summary>
    /// 🧪 ENDPOINT DE DESARROLLO - Limpia todos los locks de Redis (SOLO PARA PRUEBAS)
    /// </summary>
    /// <remarks>
    /// ⚠️ Este endpoint es SOLO para desarrollo y pruebas. NO debe usarse en producción.
    /// Limpia todos los locks activos en Redis para permitir nuevas ejecuciones.
    /// </remarks>
    /// <returns>Confirmación de limpieza</returns>
    [HttpDelete("limpiar-locks")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 503)]
    public async Task<IActionResult> LimpiarLocks()
    {
        var correlationId = CorrelationHelper.GetCorrelationId(HttpContext);

        _logger.LogWarning(
            "🧪 [BATCH_SYNC] [DEV] Solicitud de limpieza de locks. CorrelationId: {CorrelationId}",
            correlationId);
        
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine("🧪 [BATCH_SYNC] [DEV] LIMPIANDO LOCKS DE REDIS...");
        Console.WriteLine("════════════════════════════════════════════════════════════");

        try
        {
            if (_redisConnection == null || !_redisConnection.IsConnected)
            {
                var msg = "Redis no está disponible. No se pueden limpiar locks.";
                Console.WriteLine($"❌ {msg}");
                return StatusCode(503, new ApiResponse
                {
                    Success = false,
                    Message = msg,
                    Timestamp = DateTimeHelper.GetMexicoTimeString()
                });
            }

            var db = _redisConnection.GetDatabase();
            var server = _redisConnection.GetServer(_redisConnection.GetEndPoints().First());
            
            // Buscar TODAS las keys relacionadas con locks (RedLock puede usar diferentes formatos)
            var allKeys = new List<StackExchange.Redis.RedisKey>();
            
            // Patrón 1: lock:sync:*
            allKeys.AddRange(server.Keys(pattern: "lock:sync:*").ToArray());
            
            // Patrón 2: Buscar todas las keys que contengan "lock" (por si RedLock usa otro formato)
            // Esto es más agresivo pero necesario para limpiar locks bloqueados
            var allLockKeys = server.Keys(pattern: "*lock*").ToArray();
            foreach (var key in allLockKeys)
            {
                if (!allKeys.Contains(key))
                {
                    allKeys.Add(key);
                }
            }
            
            var keysDeleted = 0;
            var keysToDelete = allKeys.Distinct().ToArray();

            foreach (var key in keysToDelete)
            {
                var keyString = key.ToString();
                // Solo eliminar keys que realmente sean de locks (evitar eliminar otras keys importantes)
                if (keyString.Contains("lock:sync:") || keyString.StartsWith("lock:"))
                {
                    await db.KeyDeleteAsync(key);
                    keysDeleted++;
                    var deleteMsg = $"🧪 [BATCH_SYNC] [DEV] Key eliminada: {keyString}";
                    Console.WriteLine(deleteMsg);
                    Console.Out.Flush();
                    _logger.LogWarning(deleteMsg);
                }
            }
            
            // Si no encontramos keys con el patrón específico, intentar eliminar todas las keys relacionadas con RedLock
            if (keysDeleted == 0)
            {
                // Listar TODAS las keys en Redis para debugging
                var allRedisKeys = server.Keys(pattern: "*").ToArray();
                Console.WriteLine($"🧪 [BATCH_SYNC] [DEV] Total de keys en Redis: {allRedisKeys.Length}");
                Console.Out.Flush();
                
                foreach (var key in allRedisKeys.Take(20)) // Mostrar solo las primeras 20
                {
                    var keyStr = key.ToString();
                    Console.WriteLine($"🧪 [BATCH_SYNC] [DEV] Key encontrada: {keyStr}");
                    Console.Out.Flush();
                    
                    // Intentar eliminar keys que parezcan ser locks
                    if (keyStr.Contains("lock") || keyStr.Contains("sync") || keyStr.Contains("ProductoList"))
                    {
                        await db.KeyDeleteAsync(key);
                        keysDeleted++;
                        Console.WriteLine($"🧪 [BATCH_SYNC] [DEV] Key eliminada: {keyStr}");
                        Console.Out.Flush();
                    }
                }
                
                // También intentar eliminar directamente las keys conocidas
                var knownKeys = new[] { "lock:sync:ProductoList", "lock:sync:ProductList" };
                foreach (var knownKey in knownKeys)
                {
                    if (await db.KeyExistsAsync(knownKey))
                    {
                        await db.KeyDeleteAsync(knownKey);
                        keysDeleted++;
                        Console.WriteLine($"🧪 [BATCH_SYNC] [DEV] Key conocida eliminada: {knownKey}");
                        Console.Out.Flush();
                    }
                }
            }

            var successMsg = $"🧪 [BATCH_SYNC] [DEV] {keysDeleted} lock(s) limpiado(s) exitosamente. CorrelationId: {correlationId}";
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.WriteLine(successMsg);
            Console.WriteLine("════════════════════════════════════════════════════════════");
            _logger.LogWarning(
                "🧪 [BATCH_SYNC] [DEV] {KeysDeleted} lock(s) limpiado(s) exitosamente. CorrelationId: {CorrelationId}",
                keysDeleted, correlationId);

            return Ok(new ApiResponse
            {
                Success = true,
                Message = $"⚠️ {keysDeleted} lock(s) limpiado(s) manualmente (SOLO DESARROLLO).",
                Data = new { KeysDeleted = keysDeleted },
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
        catch (Exception ex)
        {
            var errorMsg = $"❌ [BATCH_SYNC] [DEV] Error al limpiar locks: {ex.Message}";
            Console.WriteLine(errorMsg);
            _logger.LogError(ex,
                "❌ [BATCH_SYNC] [DEV] Error al limpiar locks. CorrelationId: {CorrelationId}",
                correlationId);

            return StatusCode(500, new ApiResponse
            {
                Success = false,
                Message = $"Error al limpiar locks: {ex.Message}",
                Timestamp = DateTimeHelper.GetMexicoTimeString()
            });
        }
    }
}
