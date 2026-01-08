# 🔒 Explicación: Sincronización Concurrente en el API

## 📋 Resumen

El API de sincronización batch tiene **múltiples capas de protección** para prevenir ejecuciones concurrentes del mismo tipo de proceso. Si intentas ejecutar una sincronización mientras otra ya está en curso, el sistema **rechazará la segunda solicitud** y retornará un error `409 Conflict`.

---

## 🛡️ Capas de Protección Contra Concurrencia

### 1. **Redis Distributed Lock** (Protección Principal)

#### ¿Cómo funciona?

El sistema usa **Redis** como mecanismo de locking distribuido. Cuando intentas iniciar una sincronización:

1. El API intenta adquirir un lock en Redis con la clave: `lock:sync:{processType}`
2. Redis usa el comando **`SET key value NX EX seconds`** que es **atómico**
3. Solo **un proceso puede adquirir el lock** para el mismo `processType`

#### Ejemplo:

```
┌─────────────────────────────────────────────────────────────────┐
│ ESCENARIO: Dos solicitudes simultáneas para ProductList        │
└─────────────────────────────────────────────────────────────────┘

    Solicitud 1                    Redis                    Solicitud 2
    ───────────                    ─────                    ───────────
        │                             │                          │
        │─── SET lock:sync:ProductList ──▶                       │
        │     "uuid-123" NX EX 600   │                          │
        │                             │                          │
        │◀─── OK (lock adquirido) ────│                          │
        │                             │                          │
        │                             │                          │
        │                             │◀─── SET lock:sync:ProductList
        │                             │     "uuid-456" NX EX 600
        │                             │                          │
        │                             │─── FAIL (lock ya existe) ──▶
        │                             │                          │
        │                             │                          │
        │   Proceso continúa...       │     Solicitud 2 rechazada │
        │   (Lock renovado cada 30s)  │     con 409 Conflict      │
        │                             │                          │
```

#### Características del Lock:

- **Key única por processType**: `lock:sync:{processType}`
  - Ejemplo: `lock:sync:ProductList`
- **Expiración inicial**: 600 segundos (10 minutos)
- **Renovación automática**: Cada 30 segundos (heartbeat) mientras el proceso está activo
- **Liberación automática**: Se libera cuando el proceso finaliza (en el `finally`)

#### Código relevante:

```csharp
// DistributedLockService.cs - Línea 54-58
var lockAcquired = await db.StringSetAsync(
    key: lockKey,                    // "lock:sync:ProductList"
    value: lockValue,                // UUID único
    expiry: expiry,                  // 600 segundos
    when: When.NotExists);           // ⚠️ Solo si NO existe (NX)
```

---

### 2. **Validación en Base de Datos** (Protección Secundaria)

#### ¿Cómo funciona?

Antes de crear un nuevo registro en `CO_EVENTOSCARGASINCCONTROL`, el sistema verifica:

1. Si ya existe un proceso con el mismo `ProcessType` + `IdCarga` + `FechaCarga`
2. Si ese proceso tiene estado `PENDING` o `RUNNING`
3. Si existe, **rechaza la solicitud** con `409 Conflict`

#### Código relevante:

```csharp
// BatchSincronizacionProcesosController.cs - Líneas 248-276
var registroActivo = await _syncControlRepository.GetByProcessAsync(
    dto.ProcessType, 
    dto.IdCarga, 
    fechaCarga);

if (registroActivo != null && 
    (registroActivo.Status == "PENDING" || registroActivo.Status == "RUNNING"))
{
    // ⚠️ Rechazar - ya existe un proceso activo
    return Conflict(...);
}
```

---

### 3. **Validación de Estado SINCRONIZADA** (Protección de Idempotencia)

#### ¿Cómo funciona?

Antes de iniciar el proceso, el sistema verifica:

1. Si el proceso en `CO_EVENTOSCARGAPROCESO` tiene estado `SINCRONIZADA`
2. Si es así, **rechaza la solicitud** con `400 Bad Request` (no permite re-sincronizar)

#### Código relevante:

```csharp
// BatchSincronizacionProcesosController.cs - Líneas 222-246
var estatusProceso = await _dealerRepository.GetEventoCargaProcesoEstatusAsync(
    dto.ProcessType, 
    dto.IdCarga);

if (estatusProceso == "SINCRONIZADA")
{
    // ⚠️ Rechazar - proceso ya sincronizado
    return BadRequest(...);
}
```

---

## 🧪 Escenarios de Prueba

### Escenario 1: Dos Solicitudes Simultáneas (Mismo ProcessType)

**Solicitud 1:**
```bash
curl -X POST "https://localhost:5001/api/v1/gm/dealer-sync/batch-sincronizacion-procesos" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "processType": "ProductList",
    "idCarga": "20250107_001"
  }'
```

**Resultado:** ✅ `202 Accepted` - Proceso iniciado

---

**Solicitud 2 (inmediatamente después):**
```bash
curl -X POST "https://localhost:5001/api/v1/gm/dealer-sync/batch-sincronizacion-procesos" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "processType": "ProductList",
    "idCarga": "20250107_002"
  }'
```

**Resultado:** ❌ `409 Conflict` - Proceso ocupado

**Respuesta:**
```json
{
  "success": false,
  "message": "⚠️ PROCESO OCUPADO: El processType 'ProductList' está siendo procesado actualmente. Intente nuevamente después de que finalice el proceso actual.",
  "data": {
    "processId": "TEMP_ID_12345678",
    "lockAcquired": false,
    "processType": "ProductList",
    "idCarga": "20250107_002",
    "message": "Proceso ya en ejecución. El lock se renovará dinámicamente hasta que termine el proceso.",
    "startTime": "2025-01-07T10:30:00",
    "lockExpirySeconds": 600
  },
  "timestamp": "2025-01-07T10:30:00"
}
```

---

### Escenario 2: Dos Solicitudes Diferentes (Diferentes ProcessType)

**Solicitud 1:**
```json
{
  "processType": "ProductList",
  "idCarga": "20250107_001"
}
```

**Solicitud 2 (simultánea):**
```json
{
  "processType": "CampaignList",  // ← Diferente processType
  "idCarga": "20250107_001"
}
```

**Resultado:** ✅ **Ambas aceptadas** - Se ejecutan en paralelo porque usan **locks diferentes**

- Lock 1: `lock:sync:ProductList`
- Lock 2: `lock:sync:CampaignList`

> **Nota**: Cada `processType` tiene su propio lock independiente.

---

### Escenario 3: Solicitud Mientras Proceso Terminando

**Estado:** Un proceso está finalizando (actualizando BD a COMPLETED)

**Solicitud nueva:**
```json
{
  "processType": "ProductList",
  "idCarga": "20250107_003"
}
```

**Comportamiento:**

1. El proceso anterior libera el lock (en el `finally`)
2. La nueva solicitud intenta adquirir el lock
3. Si el lock ya se liberó: ✅ `202 Accepted` - Nueva ejecución iniciada
4. Si el lock aún no se liberó: ❌ `409 Conflict` - Esperar unos segundos

---

## 📊 Flujo de Decisión: ¿Se Permite la Ejecución?

```
┌─────────────────────────────────────────────────────────────────┐
│              DECISIÓN DE EJECUCIÓN CONCURRENTE                  │
└─────────────────────────────────────────────────────────────────┘

    ┌──────────────┐
    │ Solicitud    │
    │ POST /batch  │
    └──────┬───────┘
           │
           ▼
    ┌─────────────────────────────┐
    │ ¿Proceso ya SINCRONIZADA?   │
    └──────┬──────────────────────┘
           │
      ┌────┴────┐
      │         │
     SÍ        NO
      │         │
      ▼         ▼
    ┌─────────┐  ┌──────────────────────────┐
    │ Rechazar│  │ ¿Lock Redis disponible?  │
    │ 400 Bad │  └──────┬───────────────────┘
    │ Request │         │
    └─────────┘    ┌────┴────┐
                   │         │
                  NO        SÍ
                   │         │
                   ▼         ▼
            ┌─────────────┐  ┌─────────────────────────────┐
            │ Rechazar    │  │ ¿Existe proceso PENDING/    │
            │ 409 Conflict│  │ RUNNING en BD?              │
            └─────────────┘  └──────┬──────────────────────┘
                                    │
                               ┌────┴────┐
                               │         │
                              SÍ        NO
                               │         │
                               ▼         ▼
                        ┌─────────────┐  ┌─────────────┐
                        │ Rechazar    │  │ ✅ Permitir │
                        │ 409 Conflict│  │ Ejecución   │
                        └─────────────┘  └─────────────┘
```

---

## 🔍 Monitoreo del Estado del Lock

### Endpoint para Verificar Estado

```bash
GET /api/v1/gm/dealer-sync/batch-sincronizacion-procesos/estado/{processType}
```

**Ejemplo:**
```bash
curl -X GET "https://localhost:5001/api/v1/gm/dealer-sync/batch-sincronizacion-procesos/estado/ProductList" \
  -H "Authorization: Bearer {token}"
```

**Respuesta (Lock activo):**
```json
{
  "success": true,
  "message": "⚠️ El processType 'ProductList' tiene un lock activo. Hay un proceso en ejecución.",
  "data": {
    "processType": "ProductList",
    "lockActivo": true,
    "mensaje": "⚠️ El processType 'ProductList' tiene un lock activo..."
  },
  "timestamp": "2025-01-07T10:30:00"
}
```

**Respuesta (Lock disponible):**
```json
{
  "success": true,
  "message": "✅ El processType 'ProductList' está disponible. No hay locks activos.",
  "data": {
    "processType": "ProductList",
    "lockActivo": false,
    "mensaje": "✅ El processType 'ProductList' está disponible..."
  },
  "timestamp": "2025-01-07T10:30:00"
}
```

---

## ⚠️ Casos Especiales

### 1. ¿Qué pasa si Redis está caído?

Si Redis no está disponible, el API retorna:

```json
{
  "success": false,
  "message": "Servicio de distributed locking no disponible. Redis no está configurado o no está disponible.",
  "timestamp": "2025-01-07T10:30:00"
}
```

**Status Code:** `503 Service Unavailable`

> **⚠️ Importante**: Sin Redis, **NO hay protección contra concurrencia**. El sistema depende completamente de Redis para el distributed locking.

---

### 2. ¿Qué pasa si el proceso se cuelga?

El lock tiene una **expiración automática** (600 segundos inicialmente). Si el proceso se cuelga:

1. El lock expira después de 600 segundos (10 minutos)
2. Otras solicitudes pueden adquirir el lock después de ese tiempo
3. El proceso colgado quedará con estado `RUNNING` en BD (se puede limpiar manualmente)

**Recomendación**: Monitorear procesos `RUNNING` que duren más de 15-20 minutos.

---

### 3. ¿Qué pasa si se reinicia la aplicación?

Si la aplicación se reinicia mientras un proceso está en ejecución:

1. El lock en Redis **se mantiene** (a menos que Redis también se reinicie)
2. El proceso en Hangfire **se perderá** (no se puede continuar)
3. El registro en BD quedará con estado `RUNNING`
4. Se debe limpiar manualmente o esperar a que el lock expire

**Solución**: Usar el endpoint de limpieza de locks (solo desarrollo):

```bash
DELETE /api/v1/gm/dealer-sync/batch-sincronizacion-procesos/limpiar-locks
```

---

## 📋 Resumen de Respuestas por Escenario

| Escenario | Status Code | Mensaje |
|-----------|-------------|---------|
| ✅ Ejecución permitida | `202 Accepted` | Proceso iniciado exitosamente |
| ❌ Proceso ya SINCRONIZADA | `400 Bad Request` | Ya está sincronizado |
| ❌ Lock activo (mismo processType) | `409 Conflict` | Proceso ocupado |
| ❌ Proceso PENDING/RUNNING en BD | `409 Conflict` | Ya existe proceso activo |
| ❌ Redis no disponible | `503 Service Unavailable` | Distributed locking no disponible |
| ❌ Error inesperado | `500 Internal Server Error` | Error interno del servidor |

---

## 🔧 Recomendaciones

### Para Desarrollo

1. **Usar el endpoint de verificación** antes de ejecutar:
   ```bash
   GET /estado/{processType}
   ```

2. **Monitorear console logs** para ver el estado del lock:
   ```
   🔒 [BATCH_SYNC] Intentando adquirir lock...
   ✅ [DISTRIBUTED_LOCK] Lock adquirido exitosamente
   🔄 [REDIS_LOCK] Lock renovado exitosamente (heartbeat)
   🔓 [REDIS_LOCK] Lock de Redis liberado exitosamente
   ```

3. **Usar el endpoint de limpieza** si es necesario (solo desarrollo):
   ```bash
   DELETE /limpiar-locks
   ```

### Para Producción

1. **Monitorear Redis** - Es crítico para la protección contra concurrencia
2. **Alertar procesos RUNNING** que duren más de 20 minutos
3. **Implementar retry logic** en el cliente si recibe `409 Conflict`
4. **Usar el endpoint de verificación** antes de ejecutar procesos largos

---

## 📚 Referencias

- [DOC_API_BATCH_SINCRONIZACION_PROCESOS.md](./DOC_API_BATCH_SINCRONIZACION_PROCESOS.md) - Documentación completa del API
- [PLAN_PROYECTO_BACKEND_SINCRONIZACION.md](./PLAN_PROYECTO_BACKEND_SINCRONIZACION.md) - Plan del proyecto
- Redis SET Command: https://redis.io/commands/set/
- Distributed Locking Pattern: https://redis.io/docs/manual/patterns/distributed-locks/

