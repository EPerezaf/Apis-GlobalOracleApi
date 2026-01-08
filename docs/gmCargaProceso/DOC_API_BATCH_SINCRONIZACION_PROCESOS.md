# 📘 Documentación API: Batch Sincronización de Procesos

## Información General

| Campo | Valor |
|-------|-------|
| **Endpoint** | `POST /api/v1/gm/dealer-sync/batch-sincronizacion-procesos` |
| **Método** | POST |
| **Autenticación** | JWT Bearer Token (Requerido) |
| **Content-Type** | application/json |
| **Módulo** | GM.DealerSync |
| **Versión** | v1 |

---

## 📋 Descripción

Este endpoint inicia un proceso de sincronización batch para enviar notificaciones a los webhooks de los dealers registrados en el sistema. El proceso se ejecuta de manera asíncrona en background utilizando **Hangfire** como job scheduler y **Redis** para el control de concurrencia mediante distributed locks.

### Características Principales

- ✅ **Ejecución asíncrona**: El proceso se encola en Hangfire y retorna inmediatamente un `202 Accepted`
- ✅ **Procesamiento paralelo con TPL**: Utiliza Task Parallel Library (TPL) con `Parallel.ForEachAsync` para procesar múltiples webhooks simultáneamente
- ✅ **Pool de tareas asíncronas**: Pool de tareas administrado por .NET, completamente asíncrono y no bloqueante
- ✅ **Límite de concurrencia configurable**: Procesa 5-10 webhooks simultáneos (configurado en 5 por defecto)
- ✅ **Timeouts por cliente**: Cada webhook tiene un timeout individual de 5 minutos sin bloquear otros
- ✅ **Control de concurrencia**: Utiliza Redis RedLock para evitar ejecuciones simultáneas del mismo tipo de proceso
- ✅ **Heartbeat automático**: El lock se renueva automáticamente cada 30 segundos mientras el proceso está activo
- ✅ **Auditoría completa**: Registra inicio, fin, y estado de cada ejecución en base de datos
- ✅ **Trazabilidad**: Genera ProcessId único y registra HangfireJobId para seguimiento
- ✅ **Validación de idempotencia**: No permite ejecutar procesos que ya están sincronizados
- ✅ **Generación de payload optimizada**: El payload se genera una sola vez antes del procesamiento paralelo y se reutiliza para todos los webhooks, evitando consultas repetidas a la base de datos

---

## 🔄 Flujo del Proceso

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         FLUJO DE SINCRONIZACIÓN BATCH                        │
└─────────────────────────────────────────────────────────────────────────────┘

    ┌───────────┐     ┌─────────────┐     ┌──────────────┐     ┌────────────┐
    │  Cliente  │────▶│  API POST   │────▶│ Validaciones │────▶│ Redis Lock │
    │  (HTTP)   │     │  Endpoint   │     │  Iniciales   │     │  Acquire   │
    └───────────┘     └─────────────┘     └──────────────┘     └────────────┘
                                                                      │
                      ┌─────────────────────────────────────────────────┘
                      ▼
    ┌──────────────────────────────────────────────────────────────────────────┐
    │                          VALIDACIONES INICIALES                          │
    │  1. Validar modelo (ProcessType, IdCarga)                                │
    │  2. Validar que ProcessType esté implementado                            │
    │  3. Validar que Redis esté disponible                                    │
    │  4. Validar que no exista proceso SINCRONIZADA para el mismo IdCarga     │
    │  5. Validar que no exista proceso PENDING o RUNNING                      │
    └──────────────────────────────────────────────────────────────────────────┘
                      │
                      ▼
    ┌────────────────┐     ┌─────────────────┐     ┌──────────────────────────┐
    │ Crear registro │────▶│ Encolar Job en  │────▶│ Retornar 202 Accepted   │
    │ SyncControl    │     │    Hangfire     │     │ (Respuesta inmediata)   │
    └────────────────┘     └─────────────────┘     └──────────────────────────┘
                                  │
    ┌─────────────────────────────┘
    │  PROCESO EN BACKGROUND (Hangfire)
    ▼
    ┌──────────────────────────────────────────────────────────────────────────┐
    │                      EJECUCIÓN EN BACKGROUND                              │
    │                                                                          │
    │  ┌─────────────────┐                                                     │
    │  │ Obtener dealers │  Consulta CO_EVENTOSCARGASNAPSHOTDEALERS            │
    │  │     activos     │  agrupados por UrlWebhook                           │
    │  │   (N grupos)    │                                                     │
    │  └────────┬────────┘                                                     │
    │           │                                                              │
    │           ▼                                                              │
    │  ┌─────────────────────────────────────────────────────────────────┐    │
    │  │              📦 GENERAR PAYLOAD (UNA SOLA VEZ)                  │    │
    │  │  ┌─────────────────────────────────────────────────────────┐    │    │
    │  │  │ - Consulta productos/campañas desde BD (1 vez)          │    │    │
    │  │  │ - Construye payload completo con procesodetalle         │    │    │
    │  │  │ - Payload se genera ANTES del procesamiento paralelo    │    │    │
    │  │  │ - Se reutiliza el mismo payload para todos los webhooks │    │    │
    │  │  │ - Imprime vista previa en consola                       │    │    │
    │  │  └─────────────────────────────────────────────────────────┘    │    │
    │  └─────────────────────────────────────────────────────────────────┘    │
    │           │                                                              │
    │           ▼                                                              │
    │  ┌─────────────────────────────────────────────────────────────────┐    │
    │  │        ⚡ PROCESAMIENTO PARALELO (TPL - Parallel.ForEachAsync) │    │
    │  │                                                                 │    │
    │  │  ┌─────────────────────────────────────────────────────────┐  │    │
    │  │  │ Pool de Tareas Asíncronas (MaxDegreeOfParallelism: 5)  │  │    │
    │  │  │                                                         │  │    │
    │  │  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌─────────┐│  │    │
    │  │  │  │ Webhook  │  │ Webhook  │  │ Webhook  │  │ Webhook ││  │    │
    │  │  │  │    1     │  │    2     │  │    3     │  │    4    ││  │    │
    │  │  │  │ (PARALELO)│ (PARALELO)│ (PARALELO)│ (PARALELO)││  │    │
    │  │  │  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬────┘│  │    │
    │  │  │       │             │             │             │      │  │    │
    │  │  │       ▼             ▼             ▼             ▼      │  │    │
    │  │  │  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐  │  │    │
    │  │  │  │Obtener  │  │Obtener  │  │Obtener  │  │Obtener  │  │  │    │
    │  │  │  │dealers  │  │dealers  │  │dealers  │  │dealers  │  │  │    │
    │  │  │  │individual│  │individual│  │individual│  │individual│ │  │    │
    │  │  │  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘  │  │    │
    │  │  │       │             │             │             │      │  │    │
    │  │  │       ▼             ▼             ▼             ▼      │  │    │
    │  │  │  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐  │  │    │
    │  │  │  │POST con │  │POST con │  │POST con │  │POST con │  │  │    │
    │  │  │  │payload  │  │payload  │  │payload  │  │payload  │  │  │    │
    │  │  │  │pre-gen. │  │pre-gen. │  │pre-gen. │  │pre-gen. │  │  │    │
    │  │  │  │+Secret  │  │+Secret  │  │+Secret  │  │+Secret  │  │  │    │
    │  │  │  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘  │  │    │
    │  │  │       │             │             │             │      │  │    │
    │  │  │       ▼             ▼             ▼             ▼      │  │    │
    │  │  │  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐  │  │    │
    │  │  │  │Respuesta│  │Respuesta│  │Respuesta│  │Respuesta│  │  │    │
    │  │  │  │200+ACK  │  │401 Error│  │200+ACK  │  │Timeout  │  │  │    │
    │  │  │  │  o      │  │   o     │  │  o      │  │   o     │  │  │    │
    │  │  │  │Error    │  │200+ACK  │  │Error    │  │200+ACK  │  │  │    │
    │  │  │  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘  │  │    │
    │  │  │       │             │             │             │      │  │    │
    │  │  │       ▼             ▼             ▼             ▼      │  │    │
    │  │  │  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐  │  │    │
    │  │  │  │UPDATE BD│  │UPDATE BD│  │UPDATE BD│  │UPDATE BD│  │  │    │
    │  │  │  │(Éxito o │  │(Éxito o │  │(Éxito o │  │(Éxito o │  │  │    │
    │  │  │  │ Fallido)│  │ Fallido)│  │ Fallido)│  │ Fallido)│  │  │    │
    │  │  │  └─────────┘  └─────────┘  └─────────┘  └─────────┘  │  │    │
    │  │  │                                                         │  │    │
    │  │  │  Cuando se completa un webhook, se procesa el siguiente│  │    │
    │  │  │  (Pool mantiene máximo 5 simultáneos)                  │  │    │
    │  │  └─────────────────────────────────────────────────────────┘  │    │
    │  │                                                                 │    │
    │  │  Características:                                               │    │
    │  │  • Límite de concurrencia: 5 webhooks simultáneos (rango 5-10)│    │
    │  │  • Timeout individual: 5 minutos por webhook                  │    │
    │  │  • Thread-safe: Usa Interlocked para contadores              │    │
    │  │  • No bloqueante: Un webhook lento no afecta a otros          │    │
    │  └─────────────────────────────────────────────────────────────────┘    │
    │                                                                       │
    │           │                                                           │
    │           ▼                                                           │
    │  ┌─────────────────────────────────────────────────────────────────┐ │
    │  │                      FINALIZACIÓN                               │ │
    │  │  1. Actualizar CO_EVENTOSCARGASINCCONTROL a COMPLETED           │ │
    │  │  2. Actualizar CO_EVENTOSCARGAPROCESO (dealers sincronizados)   │ │
    │  │  3. Liberar Redis Lock                                          │ │
    │  └─────────────────────────────────────────────────────────────────┘ │
    └──────────────────────────────────────────────────────────────────────┘
```

---

## 📥 Request

### Headers Requeridos

| Header | Valor | Descripción |
|--------|-------|-------------|
| `Authorization` | `Bearer {token}` | Token JWT válido |
| `Content-Type` | `application/json` | Tipo de contenido |

### Body (JSON)

```json
{
  "processType": "ProductList",
  "idCarga": "20250107_001"
}
```

### Parámetros del Body

| Campo | Tipo | Requerido | Descripción | Validaciones |
|-------|------|-----------|-------------|--------------|
| `processType` | string | ✅ Sí | Tipo de proceso a sincronizar | Máx. 50 caracteres. Debe estar en la lista de procesos implementados |
| `idCarga` | string | ✅ Sí | Identificador único de la carga | Máx. 100 caracteres |

### Procesos Implementados

| ProcessType | Descripción |
|-------------|-------------|
| `ProductList` | Sincronización de lista de productos |

> **Nota**: Otros tipos de proceso pueden agregarse en el futuro. Si se envía un `processType` no implementado, el API retornará un error 400 con la lista de procesos disponibles.

---

## 📤 Responses

### ✅ 202 Accepted - Proceso Iniciado

El proceso se ha encolado exitosamente en Hangfire y se ejecutará en background.

```json
{
  "success": true,
  "message": "✅ Proceso de sincronización batch iniciado exitosamente y encolado en Hangfire. ProcessId: A1B2C3D4E5F6G7H8, HangfireJobId: 123. El proceso se ejecutará en background y se actualizará el estado en BD al finalizar.",
  "data": {
    "processId": "A1B2C3D4E5F6G7H8",
    "lockAcquired": true,
    "processType": "ProductList",
    "idCarga": "20250107_001",
    "message": "✅ Proceso de sincronización batch iniciado exitosamente...",
    "startTime": "2025-01-07T10:30:00",
    "lockExpirySeconds": 600
  },
  "timestamp": "2025-01-07T10:30:00"
}
```

### ⚠️ 400 Bad Request - Validación Fallida

#### Caso 1: Campos requeridos faltantes

```json
{
  "success": false,
  "message": "Validación fallida: El processType es requerido, El idCarga es requerido",
  "timestamp": "2025-01-07T10:30:00"
}
```

#### Caso 2: Proceso no implementado

```json
{
  "success": false,
  "message": "El proceso 'InvalidProcess' no está implementado o no está permitido para sincronización batch. Procesos implementados y disponibles: ProductList",
  "data": {
    "processTypeSolicitado": "InvalidProcess",
    "procesosImplementados": ["ProductList"],
    "todosLosProcesosDisponibles": ["ProductList", "CampaignList", "..."],
    "mensaje": "El proceso solicitado aún no está implementado..."
  },
  "timestamp": "2025-01-07T10:30:00"
}
```

#### Caso 3: Proceso ya sincronizado (Idempotencia)

```json
{
  "success": false,
  "message": "El proceso 'ProductList' con IdCarga '20250107_001' ya está sincronizado. Estatus actual: SINCRONIZADA. No se puede ejecutar nuevamente el proceso de sincronización.",
  "data": {
    "processType": "ProductList",
    "idCarga": "20250107_001",
    "estatus": "SINCRONIZADA",
    "eventoCargaProcesoId": 12345
  },
  "timestamp": "2025-01-07T10:30:00"
}
```

#### Caso 4: No se encontró el proceso de carga

```json
{
  "success": false,
  "message": "No se encontró un proceso de carga con ProcessType 'ProductList' e IdCarga '20250107_001'",
  "timestamp": "2025-01-07T10:30:00"
}
```

### ⚠️ 409 Conflict - Proceso en Ejecución

#### Caso 1: Lock ya adquirido por otro proceso

```json
{
  "success": false,
  "message": "⚠️ PROCESO OCUPADO: El processType 'ProductList' está siendo procesado actualmente. Intente nuevamente después de que finalice el proceso actual.",
  "data": {
    "processId": "TEMP_ID_12345678",
    "lockAcquired": false,
    "processType": "ProductList",
    "idCarga": "20250107_001",
    "message": "Proceso ya en ejecución. El lock se renovará dinámicamente hasta que termine el proceso.",
    "startTime": "2025-01-07T10:30:00",
    "lockExpirySeconds": 600
  },
  "timestamp": "2025-01-07T10:30:00"
}
```

#### Caso 2: Proceso PENDING o RUNNING existente

```json
{
  "success": false,
  "message": "Ya existe un proceso en estado 'RUNNING' para ProcessType 'ProductList' e IdCarga '20250107_001'. Debe esperar a que termine o finalice para poder ejecutarlo nuevamente.",
  "data": {
    "syncControlId": 456,
    "status": "RUNNING",
    "processType": "ProductList",
    "idCarga": "20250107_001"
  },
  "timestamp": "2025-01-07T10:30:00"
}
```

### ❌ 500 Internal Server Error

```json
{
  "success": false,
  "message": "Error interno del servidor al adquirir el lock: {detalle del error}",
  "timestamp": "2025-01-07T10:30:00"
}
```

### ❌ 503 Service Unavailable - Redis No Disponible

```json
{
  "success": false,
  "message": "Servicio de distributed locking no disponible. Redis no está configurado o no está disponible.",
  "timestamp": "2025-01-07T10:30:00"
}
```

---

## 🗄️ Tablas de Base de Datos Involucradas

### 1. CO_EVENTOSCARGASINCCONTROL (Control de Sincronización)

Registra el estado de cada ejecución del proceso de sincronización.

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `COES_SINCCONTROLID` | NUMBER | PK - ID único del registro |
| `COES_PROCESSTYPE` | VARCHAR2(50) | Tipo de proceso |
| `COES_IDCARGA` | VARCHAR2(100) | ID de la carga |
| `COES_FECHACARGA` | DATE | Fecha de la carga |
| `COES_COCP_EVENTPROCESOID` | NUMBER | FK a CO_EVENTOSCARGAPROCESO |
| `COES_HANGFIREJOBID` | VARCHAR2(100) | ID del job en Hangfire |
| `COES_STATUS` | VARCHAR2(20) | Estado: PENDING, RUNNING, COMPLETED, FAILED |
| `COES_FECHAINICIO` | DATE | Fecha/hora de inicio |
| `COES_FECHAFIN` | DATE | Fecha/hora de finalización |
| `COES_WEBHOOKSTOTALES` | NUMBER | Total de webhooks a procesar |
| `COES_WEBHOOKSPROCESADOS` | NUMBER | Webhooks procesados exitosamente |
| `COES_WEBHOOKSFALLIDOS` | NUMBER | Webhooks que fallaron |
| `COES_WEBHOOKSOMITIDOS` | NUMBER | Webhooks omitidos |
| `COES_ERRORMESSAGE` | VARCHAR2(1000) | Mensaje de error |
| `COES_ERRORDETAILS` | CLOB | Detalles del error (stack trace) |

### 2. CO_EVENTOSCARGASNAPSHOTDEALERS (Snapshot de Dealers)

Contiene los dealers a sincronizar, agrupados por webhook.

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `COSD_SNAPSHOTDEALERID` | NUMBER | PK |
| `COSD_COCP_EVENTOCARGAPROCESOID` | NUMBER | FK a CO_EVENTOSCARGAPROCESO |
| `COSD_DEALERBAC` | VARCHAR2(100) | Código BAC del dealer |
| `COSD_NOMBREDEALER` | VARCHAR2(400) | Nombre del dealer |
| `COSD_URLWEBHOOK` | VARCHAR2(1000) | URL del webhook |
| `COSD_SECRETKEY` | VARCHAR2(500) | Secret key para autenticación |
| `COSD_DMS` | VARCHAR2(100) | Sistema DMS origen |
| `COSD_ESTADOWEBHOOK` | VARCHAR2(20) | Estado: PENDIENTE, ENVIADO, EXITOSO, FALLIDO |
| `COSD_INTENTOSWEBHOOK` | NUMBER | Número de intentos |
| `COSD_ULTIMOINTENTOWEBHOOK` | DATE | Fecha del último intento |
| `COSD_ULTIMOERRORWEBHOOK` | VARCHAR2(1000) | Último error registrado |

### 3. CO_SINCRONIZACIONCARGAPROCESODEALER (Registros de Sincronización)

Registra cada dealer sincronizado exitosamente con su ACK token.

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `COSC_SINCARGAPROCESODEALERID` | NUMBER | PK |
| `COSC_COCP_EVENTOCARGAPROCESOID` | NUMBER | FK a CO_EVENTOSCARGAPROCESO |
| `COSC_DMSORIGEN` | VARCHAR2(400) | Sistema DMS origen |
| `COSC_DEALERBAC` | VARCHAR2(100) | Código BAC del dealer |
| `COSC_NOMBREDEALER` | VARCHAR2(400) | Nombre del dealer |
| `COSC_FECHASINCRONIZACION` | DATE | Fecha/hora de sincronización |
| `COSC_REGISTROSSINCRONIZADOS` | NUMBER | Contador de registros sincronizados |
| `COSC_TOKENCONFIRMACION` | VARCHAR2(100) | ACK Token recibido del webhook |

### 4. CO_EVENTOSCARGAPROCESO (Proceso de Carga)

Tabla principal del proceso de carga, se actualiza con estadísticas de sincronización.

| Columna | Tipo | Descripción |
|---------|------|-------------|
| `COCP_EVENTOCARGAPROCESOID` | NUMBER | PK |
| `COCP_PROCESO` | VARCHAR2(50) | Tipo de proceso |
| `COCP_IDCARGA` | VARCHAR2(100) | ID de la carga |
| `COCP_ESTATUS` | VARCHAR2(50) | Estado del proceso |
| `COCP_DEALERSSINCRONIZADOS` | NUMBER | Total de dealers sincronizados |
| `COCP_PORCDEALERSSINC` | NUMBER | Porcentaje de sincronización |

---

## 🔐 Autenticación de Webhooks

Cada webhook se llama con el header de autenticación y el payload completo generado previamente:

```http
POST {urlWebhook}
Content-Type: application/json
X-Webhook-Secret: {secretKey}

{
  "procesodetalle": [
    {
      "eventoCargaProcesoId": 24,
      "proceso": "ProductList",
      "fechaCarga": "2025-12-31T13:13:54",
      "idCarga": "productlist_31122025_1313",
      "registros": 341,
      "webhooksTotales": 47
    }
  ],
  "listaProductos": [
    {
      "nombreProducto": "2026 BUICK Envision",
      "pais": "Mexico",
      "nombreModelo": "Envision",
      "anioModelo": 2026,
      "modeloInteres": "BUENV012026",
      "marcaNegocio": "BUICK",
      "nombreLocal": "Envision",
      "definicionVehiculo": "2026 BUICK Envision"
    },
    ...
  ]
}
```

> **Nota**: El payload se genera **una sola vez** antes del procesamiento paralelo y se reutiliza para todos los webhooks. Esto optimiza el rendimiento al evitar consultas repetidas a la base de datos durante el procesamiento paralelo.

### Respuesta Esperada del Webhook

```json
{
  "ackToken": "ACK-abc123def456..."
}
```

> **Nota**: El sistema acepta las propiedades `ackToken`, `ack_token` o `tokenConfirmacion` en la respuesta. Si ninguna está presente, se genera un ACK token automáticamente.

---

## 🔄 Estados del Proceso

| Estado | Descripción |
|--------|-------------|
| `PENDING` | Proceso creado, esperando ejecución |
| `RUNNING` | Proceso en ejecución activa |
| `COMPLETED` | Proceso finalizado exitosamente |
| `FAILED` | Proceso finalizado con error |

### Estados de Webhook por Dealer

| Estado | Descripción |
|--------|-------------|
| `PENDIENTE` | Webhook no enviado aún |
| `ENVIADO` | Webhook enviado, esperando respuesta |
| `EXITOSO` | Webhook procesado correctamente (200 + ACK) |
| `FALLIDO` | Webhook falló (error de conexión, auth, etc.) |

---

## 🛠️ Endpoints Adicionales

### Verificar Estado del Lock

```http
GET /api/v1/gm/dealer-sync/batch-sincronizacion-procesos/estado/{processType}
Authorization: Bearer {token}
```

**Respuesta:**
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

### Limpiar Locks (Solo Desarrollo)

```http
DELETE /api/v1/gm/dealer-sync/batch-sincronizacion-procesos/limpiar-locks
Authorization: Bearer {token}
```

> ⚠️ **Advertencia**: Este endpoint es solo para desarrollo y pruebas. No debe usarse en producción.

---

## 📊 Métricas y Monitoreo

### Console Logs (Desarrollo)

El proceso genera logs detallados en consola durante la ejecución:

```
════════════════════════════════════════════════════════════
✅ [BATCH_SYNC] PROCESO INICIADO
✅ [BATCH_SYNC] ProcessId: A1B2C3D4E5F6G7H8
✅ [BATCH_SYNC] ProcessType: ProductList
✅ [BATCH_SYNC] IdCarga: 20250107_001
✅ [BATCH_SYNC] SyncControlId: 123
✅ [BATCH_SYNC] HangfireJobId: 456
✅ [DISTRIBUTED_LOCK] Lock adquirido exitosamente
════════════════════════════════════════════════════════════

📋 [BATCH_SYNC] LISTA DE DEALERS A SINCRONIZAR: 10 dealers total
📋 [BATCH_SYNC] [  1/10] DealerBAC: ABC123 | Estado: PENDIENTE | URL: https://...

════════════════════════════════════════════════════════════
📦 [PAYLOAD] Generando payload para ProcessType: ProductList...
════════════════════════════════════════════════════════════
✅ [PAYLOAD] Payload generado exitosamente - Listo para enviar a webhooks

📄 [PAYLOAD] Vista previa del payload generado:
───────────────────────────────────────────────────────────────
{
  "procesodetalle": [
    {
      "eventoCargaProcesoId": 24,
      "proceso": "ProductList",
      "fechaCarga": "2025-12-31T13:13:54",
      "idCarga": "productlist_31122025_1313",
      "registros": 341,
      "webhooksTotales": 47
    }
  ],
  "listaProductos": [
    {
      "nombreProducto": "2026 BUICK Envision",
      "pais": "Mexico",
      ...
    }
  ]
}
───────────────────────────────────────────────────────────────

════════════════════════════════════════════════════════════
🔄 [BATCH_SYNC] Iniciando procesamiento PARALELO de 5 webhooks...
⚡ [TPL] Usando Task Parallel Library (Pool de tareas asíncronas)
⚡ [CONCURRENCIA] Límite: 5 webhooks simultáneos (rango recomendado: 5-10)
⚡ [TIMEOUT] Timeout por webhook: 5 minutos
════════════════════════════════════════════════════════════

🌐 [WEBHOOK] Webhook 1/5: Procesando webhook (PARALELO)
   URL: https://dealer-webhook.example.com/sync
   DealerBACs: ABC123, DEF456
   ✅ Respuesta: StatusCode 200 - Sincronización EXITOSA
   🎫 ACK Token: ACK-abc123def456...

════════════════════════════════════════════════════════════
📊 [BATCH_SYNC] RESUMEN FINAL
════════════════════════════════════════════════════════════

🌐 WEBHOOKS:
   📦 Total de webhooks procesados: 5
   ✅ Total de webhooks exitosos: 3
   ❌ Total de webhooks con error: 1
   ⏭️  Total de webhooks omitidos: 1

👥 DEALERS:
   📦 Total de dealers: 10
   ✅ Dealers sincronizados: 6
   ❌ Dealers con error: 2
   ⏭️  Dealers omitidos: 2
════════════════════════════════════════════════════════════
```

### Hangfire Dashboard

El proceso puede monitorearse desde el dashboard de Hangfire:
- URL: `https://{host}/hangfire`
- Jobs encolados, en ejecución, completados y fallidos
- Reintentos automáticos configurables

---

## ⚙️ Configuración

### appsettings.json

```json
{
  "ConnectionStrings": {
    "Oracle": "User Id=...;Password=...;Data Source=..."
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Jwt": {
    "Key": "...",
    "Issuer": "...",
    "Audience": "..."
  }
}
```

### Constantes del Proceso

| Constante | Valor | Descripción |
|-----------|-------|-------------|
| `LOCK_INITIAL_EXPIRY_SECONDS` | 600 | Tiempo inicial del lock (10 minutos) |
| `LOCK_RENEWAL_INTERVAL_SECONDS` | 30 | Intervalo de renovación del heartbeat |
| `LOCK_RENEWAL_EXPIRY_SECONDS` | 600 | Tiempo de renovación del lock |
| `MAX_PARALLEL_WEBHOOKS` | 5 | Máximo de webhooks procesados simultáneamente (rango recomendado: 5-10) |
| `WEBHOOK_TIMEOUT_MINUTES` | 5 | Timeout individual por webhook (no bloquea otros webhooks) |

---

## 📦 Generación de Payload

### Optimización: Payload Generado Una Sola Vez

El sistema genera el payload completo **una sola vez** antes de iniciar el procesamiento paralelo de webhooks. Esto optimiza significativamente el rendimiento al evitar consultas repetidas a la base de datos durante el procesamiento.

### Flujo de Generación

1. **Obtención de dealers activos**: Se consultan los dealers agrupados por `UrlWebhook` desde `CO_EVENTOSCARGASNAPSHOTDEALERS`
2. **Generación del payload**: Según el `processType`, se consulta la base de datos una sola vez:
   - **ProductList**: Consulta `CO_GM_LISTAPRODUCTOS` y genera `listaProductos`
   - **CampaignList**: Consulta `LABGDMS.CO_CAMPAIGNCATALOG` y genera `listaCampanias`
3. **Construcción del procesodetalle**: Se incluye información del proceso (ID, fecha, registros, dealers totales)
4. **Reutilización**: El payload generado se reutiliza para todos los webhooks durante el procesamiento paralelo

### Estructura del Payload

#### Para ProductList

```json
{
  "procesodetalle": [
    {
      "eventoCargaProcesoId": 24,
      "proceso": "ProductList",
      "fechaCarga": "2025-12-31T13:13:54",
      "idCarga": "productlist_31122025_1313",
      "registros": 341,
      "webhooksTotales": 47
    }
  ],
  "listaProductos": [
    {
      "nombreProducto": "2026 BUICK Envision",
      "pais": "Mexico",
      "nombreModelo": "Envision",
      "anioModelo": 2026,
      "modeloInteres": "BUENV012026",
      "marcaNegocio": "BUICK",
      "nombreLocal": "Envision",
      "definicionVehiculo": "2026 BUICK Envision"
    },
    ...
  ]
}
```

#### Para CampaignList

```json
{
  "procesodetalle": [
    {
      "eventoCargaProcesoId": 24,
      "proceso": "CampaignList",
      "fechaCarga": "2025-12-31T13:13:54",
      "idCarga": "campaignlist_31122025_1313",
      "registros": 150,
      "webhooksTotales": 47
    }
  ],
  "listaCampanias": [
    {
      "sourceCodeId": "SC001",
      "id": "CAMPAIGN_001",
      "name": "Campaña Promocional",
      "recordTypeId": "RT001",
      "leadRecordType": "Lead",
      "leadEnquiryType": "Enquiry",
      "leadSource": "Web",
      "leadSourceDetails": "Landing Page",
      "status": "Active"
    },
    ...
  ]
}
```

### Beneficios de la Generación Anticipada

| Ventaja | Descripción |
|---------|-------------|
| **Rendimiento** | Evita N consultas a BD (una por webhook) → Solo 1 consulta total |
| **Consistencia** | Todos los webhooks reciben exactamente los mismos datos |
| **Eficiencia** | Reduce la carga en la base de datos durante el procesamiento paralelo |
| **Debugging** | El payload se imprime en consola para facilitar el debugging |

### Visualización en Consola

El sistema imprime una vista previa del payload generado (primeros 2000 caracteres) antes de iniciar el procesamiento paralelo:

```
════════════════════════════════════════════════════════════
📦 [PAYLOAD] Generando payload para ProcessType: ProductList...
════════════════════════════════════════════════════════════
✅ [PAYLOAD] Payload generado exitosamente - Listo para enviar a webhooks

📄 [PAYLOAD] Vista previa del payload generado:
───────────────────────────────────────────────────────────────
{
  "procesodetalle": [
    {
      "eventoCargaProcesoId": 24,
      "proceso": "ProductList",
      ...
    }
  ],
  "listaProductos": [...]
}
───────────────────────────────────────────────────────────────
```

---

## ⚡ Procesamiento Paralelo con TPL (Task Parallel Library)

### Arquitectura de Procesamiento Paralelo

El sistema utiliza **Task Parallel Library (TPL)** de .NET para procesar múltiples webhooks de forma simultánea, mejorando significativamente el rendimiento y reduciendo el tiempo total de sincronización.

#### Componentes Principales

1. **Parallel.ForEachAsync**: Método principal para procesamiento paralelo asíncrono
2. **Pool de Tareas Asíncronas**: Administrado automáticamente por .NET Runtime
3. **Límite de Concurrencia**: Control del número máximo de webhooks simultáneos
4. **Thread-Safety**: Uso de `Interlocked` y locks para contadores y logs seguros

### Configuración de Concurrencia

```csharp
// Límite de concurrencia configurable
private const int MAX_PARALLEL_WEBHOOKS = 5; // Rango recomendado: 5-10

// Configuración de ParallelOptions
var parallelOptions = new ParallelOptions
{
    MaxDegreeOfParallelism = MAX_PARALLEL_WEBHOOKS
};
```

**Recomendaciones de Configuración:**

| Webhooks Totales | Límite Recomendado | Justificación |
|------------------|-------------------|---------------|
| 1-10 | 5 | Balance óptimo para cargas pequeñas |
| 11-50 | 5-7 | Evita saturación de red/BD |
| 51-100 | 7-10 | Maximiza throughput sin sobrecargar |
| 100+ | 10 | Máximo recomendado para estabilidad |

### Beneficios del Procesamiento Paralelo

#### ⚡ Mejora de Performance

**Ejemplo con 20 webhooks (cada uno tarda ~30 segundos):**

| Modo | Tiempo Estimado | Mejora |
|------|----------------|--------|
| **Secuencial** | 20 × 30s = **10 minutos** | - |
| **Paralelo (5)** | 4 batches × 30s = **2 minutos** | **5x más rápido** |
| **Paralelo (10)** | 2 batches × 30s = **1 minuto** | **10x más rápido** |

#### 🛡️ Resiliencia y Aislamiento

- **Timeouts independientes**: Si un webhook tarda 5 minutos, los demás continúan procesándose
- **Aislamiento de errores**: Un webhook fallido no afecta a los demás
- **No bloqueo mutuo**: Cada webhook se procesa de forma independiente

#### 📊 Escalabilidad Controlada

- **Evita saturar la red**: Limita el número de conexiones simultáneas
- **Protege la base de datos**: Controla la carga concurrente de escritura
- **Previene caídas en cascada**: Evita sobrecargar servidores remotos

### Timeouts y Circuit Breakers

#### Timeout por Webhook

Cada webhook tiene un **timeout individual de 5 minutos** configurado en `HttpClient`:

```csharp
_httpClient.Timeout = TimeSpan.FromMinutes(5);
```

**Características:**
- ✅ No bloquea otros webhooks si uno se demora
- ✅ Permite procesar catálogos grandes (puede tardar varios minutos)
- ✅ Con procesamiento paralelo, otros webhooks continúan normalmente

#### Circuit Breakers (Futuro)

El sistema está preparado para implementar **Circuit Breakers** usando Polly:

```csharp
// TODO: Implementar con Polly
// - Detectar webhooks fallidos repetidamente
// - Abrir el circuito temporalmente
// - Implementar backoff exponencial para reintentos
```

**Beneficios de Circuit Breakers:**
- Reduce llamadas a webhooks que están caídos
- Implementa backoff exponencial automático
- Mejora la resiliencia del sistema

### Thread-Safety y Contadores

El sistema garantiza thread-safety en el procesamiento paralelo:

```csharp
// Contadores thread-safe usando Interlocked
var webhooksProcesados = 0;
var dealersSincronizados = 0;

// Incrementos atómicos
Interlocked.Increment(ref webhooksProcesados);
Interlocked.Add(ref dealersSincronizados, dealersIndividuales.Count);

// Locks para logs de consola
lock (lockContador)
{
    Console.WriteLine($"✅ Webhook {numero} completado...");
}
```

### Ejemplo de Ejecución Paralela

```
════════════════════════════════════════════════════════════
🔄 [BATCH_SYNC] Iniciando procesamiento PARALELO de 20 webhooks...
⚡ [TPL] Usando Task Parallel Library (Pool de tareas asíncronas)
⚡ [CONCURRENCIA] Límite: 5 webhooks simultáneos (rango recomendado: 5-10)
⚡ [TIMEOUT] Timeout por webhook: 5 minutos
════════════════════════════════════════════════════════════

Batch 1 (Webhooks 1-5):   █████ (procesando en paralelo)
Batch 2 (Webhooks 6-10):  █████ (esperando... luego procesando)
Batch 3 (Webhooks 11-15): █████ (esperando... luego procesando)
Batch 4 (Webhooks 16-20): █████ (esperando... luego procesando)

⏱️ Tiempo total: ~2 minutos (vs 10 minutos secuencial)
```

### Monitoreo del Procesamiento Paralelo

Los logs muestran el estado de cada webhook procesado en paralelo:

```
🌐 [WEBHOOK] Webhook 1/20: Procesando webhook (PARALELO)
   URL: https://dealer1.example.com/webhook
   ✅ Respuesta: StatusCode 200 - Sincronización EXITOSA

🌐 [WEBHOOK] Webhook 2/20: Procesando webhook (PARALELO)
   URL: https://dealer2.example.com/webhook
   ✅ Respuesta: StatusCode 200 - Sincronización EXITOSA

... (otros webhooks procesándose simultáneamente) ...
```

---

## 🧪 Simulación (Modo Desarrollo)

Cuando el webhook real falla con error de autenticación (401/403), el sistema activa un modo de simulación que genera resultados aleatorios:

| Escenario | Probabilidad | Resultado |
|-----------|--------------|-----------|
| Éxito | 50% | 200 OK + ACK Token (con delay de 3-10 segundos) |
| Error Auth | 30% | 401/403 Unauthorized |
| Error Conexión | 20% | Timeout/Connection refused |

> **Nota**: Esta simulación se desactivará cuando los webhooks reales estén disponibles.

---

## 📝 Ejemplo de Uso con cURL

```bash
curl -X POST "https://localhost:5001/api/v1/gm/dealer-sync/batch-sincronizacion-procesos" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json" \
  -d '{
    "processType": "ProductList",
    "idCarga": "20250107_001"
  }'
```

---

## 📚 Referencias

- [PLAN_PROYECTO_BACKEND_SINCRONIZACION.md](./PLAN_PROYECTO_BACKEND_SINCRONIZACION.md) - Plan detallado del proyecto
- [LOCAL_SYNC_CONTROL_TABLE.sql](./scripts/LOCAL_SYNC_CONTROL_TABLE.sql) - Script de creación de tabla
- [EXPLICACION_CONCURRENCIA_SINCRONIZACION.md](./EXPLICACION_CONCURRENCIA_SINCRONIZACION.md) - Explicación de concurrencia y locks
- Hangfire Documentation: https://www.hangfire.io/
- Redis RedLock: https://redis.io/docs/manual/patterns/distributed-locks/
- Task Parallel Library (TPL): https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-parallel-library-tpl
- Parallel.ForEachAsync: https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.parallel.foreachasync

---

## 📅 Historial de Cambios

| Fecha | Versión | Descripción |
|-------|---------|-------------|
| 2025-01-07 | 1.0.0 | Versión inicial del documento |
| 2025-01-07 | 1.1.0 | Agregada sección de procesamiento paralelo con TPL (Task Parallel Library). Documentación de Parallel.ForEachAsync, límites de concurrencia (5-10), timeouts por cliente, y beneficios de performance |
| 2025-01-07 | 1.2.0 | Agregada sección de generación de payload. Documentación de cómo el payload se genera una sola vez antes del procesamiento paralelo, estructura del payload para ProductList y CampaignList, y visualización en consola. Actualizado flujo del proceso y ejemplo de payload en autenticación de webhooks |

