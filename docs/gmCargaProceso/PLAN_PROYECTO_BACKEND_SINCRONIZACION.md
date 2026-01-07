# Plan de Proyecto: Script Backend Sincronizacion

## 📋 Resumen Ejecutivo

Este documento describe el plan paso a paso para desarrollar el proyecto **"Script Backend Sincronizacion"**, un servicio backend en ASP.NET Core que actúa como **receptor de webhooks** para sincronización de catálogos desde el sistema central hacia los distribuidores (dealers).

**Objetivo:** Implementar el lado del receptor del webhook (Dealer Webhook API) que recibe, valida, procesa y sincroniza actualizaciones de catálogos enviadas por el sistema central.

**Tecnología:** ASP.NET Core 9.0 (C#)  
**Arquitectura:** Modular Monolith (siguiendo convenciones de GlobalOracleAPI)  
**Base de Datos:** Oracle (usando Dapper como ORM)

---

## 🎯 Contexto y Relación con el Sistema Existente

### Sistema Central (GM.CatalogSync.API)
- **Rol:** Emisor de webhooks
- **Responsabilidad:** Enviar notificaciones de actualización de catálogos a los dealers
- **Componentes existentes:**
  - `EventoCargaSnapshotDealer`: Contiene `UrlWebhook` y `SecretKey` por dealer
  - `Distribuidor`: Entidad con información de dealers y sus webhooks
  - Background services para procesamiento en segundo plano

### Nuevo Proyecto (Script Backend Sincronizacion)
- **Rol:** Receptor de webhooks
- **Responsabilidad:** Recibir, validar y procesar actualizaciones de catálogos
- **Ubicación propuesta:** `src/Companies/GM/DealerSync/` o `src/Companies/GM/WebhookReceiver/`

### Tecnologías Clave para el Proyecto
- **Redis RedLock:** Para implementar semáforo/candado distribuido y evitar ejecuciones concurrentes del mismo proceso
- **Hangfire:** Para ejecución de jobs en background, reintentos automáticos y dashboard de monitoreo

---

## 📐 Arquitectura Propuesta

### Estructura de Carpetas
```
src/Companies/GM/DealerSync/
├── GM.DealerSync.API/              # Capa de presentación (Controllers, Program.cs)
│   ├── Controllers/
│   │   └── Webhook/
│   │       └── WebhookActualizacionProcesoController.cs
│   ├── Middleware/
│   │   ├── WebhookAuthenticationMiddleware.cs
│   │   └── ErrorHandlingMiddleware.cs
│   └── Program.cs
│
├── GM.DealerSync.Application/      # Capa de lógica de negocio
│   ├── DTOs/
│   │   ├── WebhookPayloadDto.cs
│   │   ├── WebhookHeaderDto.cs
│   │   ├── WebhookDetailDto.cs
│   │   └── WebhookAckResponseDto.cs
│   ├── Services/
│   │   ├── IWebhookService.cs
│   │   ├── WebhookService.cs
│   │   ├── ISyncHandlerFactory.cs
│   │   ├── SyncHandlerFactory.cs
│   │   ├── ISyncHandler.cs
│   │   ├── ProductListSyncHandler.cs
│   │   ├── CampaignListSyncHandler.cs
│   │   ├── IDistributedLockService.cs
│   │   ├── DistributedLockService.cs
│   │   ├── IBatchSyncJobService.cs
│   │   └── BatchSyncJobService.cs
│   └── Validators/
│       └── WebhookPayloadValidator.cs
│
├── GM.DealerSync.Domain/           # Capa de dominio
│   ├── Entities/
│   │   ├── SyncControl.cs          # Tabla LOCAL_SYNC_CONTROL
│   │   └── SyncLog.cs              # Tabla LOCAL_SYNC_LOG (opcional)
│   └── Interfaces/
│       ├── ISyncControlRepository.cs
│       └── IProductRepository.cs   # Para UPSERTs de productos
│
└── GM.DealerSync.Infrastructure/   # Capa de infraestructura
    └── Repositories/
        ├── SyncControlRepository.cs
        └── ProductRepository.cs
```

---

## 🗄️ Modelo de Datos

### Tabla: LOCAL_SYNC_CONTROL
```sql
CREATE TABLE LOCAL_SYNC_CONTROL (
    SYNC_CONTROL_ID NUMBER PRIMARY KEY,
    PROCESS_TYPE VARCHAR2(50) NOT NULL,        -- productList, campaignList, etc.
    ID_CARGA VARCHAR2(100) NOT NULL,          -- IdCarga del proceso
    FECHA_CARGA DATE NOT NULL,                 -- FechaCarga del proceso
    VERSION NUMBER NOT NULL,                   -- Versión del catálogo
    LAST_PROCESSED_TIMESTAMP DATE,             -- Última vez procesado
    STATUS VARCHAR2(20),                      -- SUCCESS, FAILED, IGNORED, PENDING
    RECORDS_RECEIVED NUMBER,                   -- Cantidad de registros recibidos
    RECORDS_PROCESSED NUMBER,                  -- Cantidad de registros procesados
    ACK_TOKEN VARCHAR2(100),                   -- Token ACK generado
    ERROR_MESSAGE VARCHAR2(2000),              -- Mensaje de error si falló
    FECHA_ALTA DATE DEFAULT SYSDATE,
    USUARIO_ALTA VARCHAR2(50),
    FECHA_MODIFICACION DATE,
    USUARIO_MODIFICACION VARCHAR2(50),
    CONSTRAINT UQ_SYNC_CONTROL UNIQUE (PROCESS_TYPE, ID_CARGA, FECHA_CARGA)
);
```

### Tabla: LOCAL_SYNC_LOG (Opcional - para auditoría)
```sql
CREATE TABLE LOCAL_SYNC_LOG (
    SYNC_LOG_ID NUMBER PRIMARY KEY,
    SYNC_CONTROL_ID NUMBER,
    REQUEST_PAYLOAD CLOB,                      -- Payload completo recibido
    RESPONSE_PAYLOAD CLOB,                     -- Respuesta ACK generada
    HTTP_STATUS_CODE NUMBER,
    PROCESSING_TIME_MS NUMBER,
    ERROR_DETAILS CLOB,
    FECHA_REGISTRO DATE DEFAULT SYSDATE,
    FOREIGN KEY (SYNC_CONTROL_ID) REFERENCES LOCAL_SYNC_CONTROL(SYNC_CONTROL_ID)
);
```

---

## 📝 Plan de Implementación Paso a Paso

### **FASE 1: Configuración Inicial y Estructura Base**

#### Paso 1.1: Crear estructura del proyecto
- [ ] Crear solución y proyectos siguiendo estructura modular
- [ ] Configurar `.csproj` con dependencias base:
  - ASP.NET Core 9.0
  - Dapper
  - Oracle.ManagedDataAccess.Core
  - Serilog
  - FluentValidation (opcional)
  - **RedLock.net** - Para distributed locking con Redis
  - **StackExchange.Redis** - Cliente Redis para RedLock
  - **Hangfire.Core** - Core de Hangfire
  - **Hangfire.AspNetCore** - Integración con ASP.NET Core
  - **Hangfire.SqlServer** o **Hangfire.PostgreSql** - Storage para Hangfire (o usar Redis)
- [ ] Configurar `Program.cs` con:
  - Serilog
  - Swagger/Scalar
  - CORS
  - Dependency Injection básico
  - **Redis Connection** - Configurar conexión a Redis
  - **Hangfire** - Configurar Hangfire con storage y dashboard

#### Paso 1.2: Configurar base de datos local
- [ ] Crear tablas `LOCAL_SYNC_CONTROL` y `LOCAL_SYNC_LOG` en Oracle
- [ ] Crear secuencias para IDs
- [ ] Configurar `IOracleConnectionFactory` en `Program.cs`
- [ ] Crear `appsettings.json` con connection strings:
  - Oracle connection string
  - **Redis connection string** (para RedLock)
  - **Hangfire storage connection** (SQL Server, PostgreSQL o Redis)

#### Paso 1.3: Crear entidades de dominio
- [ ] `SyncControl.cs` - Entidad para tabla LOCAL_SYNC_CONTROL
- [ ] `SyncLog.cs` - Entidad para tabla LOCAL_SYNC_LOG (opcional)
- [ ] Interfaces de repositorio básicas

#### Paso 1.4: Configurar Redis y RedLock
- [ ] Instalar y configurar Redis (local o servidor)
- [ ] Configurar `StackExchange.Redis` en `Program.cs`
- [ ] Crear `IDistributedLockService` e implementación con RedLock.net
- [ ] Implementar métodos:
  - `AcquireLockAsync(string lockKey, TimeSpan expiry)`
  - `ReleaseLockAsync(string lockKey)`
  - `ExtendLockAsync(string lockKey, TimeSpan additionalTime)`
- [ ] Configurar timeout y retry policy para RedLock

#### Paso 1.5: Configurar Hangfire
- [ ] Configurar Hangfire en `Program.cs`:
  - Configurar storage (SQL Server, PostgreSQL o Redis)
  - Configurar servidor de Hangfire
  - Habilitar dashboard de Hangfire (`/hangfire`)
- [ ] Configurar autenticación para dashboard (solo usuarios autorizados)
- [ ] Configurar opciones de Hangfire:
  - Worker count
  - Queue configuration
  - Retry attempts
  - Job expiration time
- [ ] Crear `IBatchSyncJobService` para encapsular lógica de jobs

---

### **FASE 2: Sistema de Lock Distribuido y Jobs en Background**

#### Paso 2.1: Implementar DistributedLockService
- [ ] Crear `IDistributedLockService.cs` con métodos:
  - `AcquireLockAsync(string processType, TimeSpan expiry)`
  - `ReleaseLockAsync(string lockKey)`
  - `IsLockAcquiredAsync(string lockKey)`
- [ ] Implementar `DistributedLockService.cs` usando RedLock.net:
  - Usar `RedLockFactory` para crear locks
  - Implementar lógica de adquisición de lock con retry
  - Manejar expiración automática de locks
  - Logging de adquisición/liberación de locks
- [ ] Generar `lockKey` basado en `processType` (ej: `"sync-lock:productList"`)

#### Paso 2.2: Implementar BatchSyncJobService con Hangfire
- [ ] Crear `IBatchSyncJobService.cs` con métodos:
  - `EnqueueSyncJobAsync(string processType, WebhookPayloadDto payload)`
  - `ScheduleRetryJobAsync(string processType, WebhookPayloadDto payload, DateTime scheduleAt)`
  - `GetJobStatusAsync(string jobId)`
- [ ] Implementar `BatchSyncJobService.cs`:
  - Usar `BackgroundJob.Enqueue()` para jobs inmediatos
  - Usar `BackgroundJob.Schedule()` para jobs programados
  - Usar `BackgroundJob.ContinueJobWith()` para jobs encadenados
  - Configurar reintentos automáticos con `[AutomaticRetry]`
- [ ] Implementar método de procesamiento del job:
  - Adquirir lock distribuido antes de procesar
  - Ejecutar lógica de sincronización
  - Liberar lock al finalizar (éxito o error)

#### Paso 2.3: Integrar lock y jobs en el flujo
- [ ] Modificar endpoint del webhook para:
  - Intentar adquirir lock antes de encolar job
  - Si lock está ocupado → Responder `409 Conflict - Proceso ya en ejecución`
  - Si lock se adquiere → Encolar job en Hangfire y responder `202 Accepted`
- [ ] Implementar liberación automática de lock:
  - Al completar el job exitosamente
  - Al fallar el job (con timeout)
  - En caso de excepción no manejada

---

### **FASE 3: Endpoint del Webhook y Validación de Seguridad**

#### Paso 3.1: Crear Controller del Webhook
- [ ] Crear `WebhookActualizacionProcesoController.cs`
- [ ] Implementar endpoint `POST /webhook/actualizacion-proceso`
- [ ] **Integrar con DistributedLockService:**
  - Intentar adquirir lock antes de procesar
  - Si lock está ocupado → `409 Conflict - Proceso ya en ejecución`
  - Si lock se adquiere → Encolar job en Hangfire
- [ ] **Integrar con Hangfire:**
  - Usar `IBatchSyncJobService` para encolar job
  - Retornar `202 Accepted` con `jobId` en la respuesta
- [ ] Documentación XML completa (siguiendo .cursorrules)
- [ ] Configurar `[ProducesResponseType]` para todos los códigos HTTP:
  - `202 Accepted` - Job encolado exitosamente
  - `409 Conflict` - Proceso ya en ejecución (lock ocupado)

#### Paso 3.2: Crear DTOs de Request/Response
- [ ] `WebhookPayloadDto.cs` - DTO principal del payload
- [ ] `WebhookHeaderDto.cs` - Cabecera del proceso
- [ ] `WebhookDetailDto.cs` - Detalle del catálogo
- [ ] `WebhookAckResponseDto.cs` - Respuesta ACK
- [ ] Data Annotations para validación básica

#### Paso 3.3: Implementar validación de seguridad
- [ ] Crear `WebhookAuthenticationMiddleware.cs` o `IAuthorizationFilter`
- [ ] Validar `X-Webhook-Secret` header contra configuración
- [ ] O validar JWT Token (si se usa JWT):
  - Validar firma
  - Validar expiración
  - Validar issuer/audience
- [ ] Responder `401 Unauthorized` o `403 Forbidden` si falla
- [ ] Registrar intentos fallidos en logs

#### Paso 3.4: Implementar manejo de errores global
- [ ] Crear `ErrorHandlingMiddleware.cs`
- [ ] Mapear excepciones a códigos HTTP apropiados
- [ ] Formatear respuestas de error consistentes
- [ ] Logging estructurado de errores

---

### **FASE 4: Validación Estructural del Payload**

#### Paso 4.1: Validación con Data Annotations
- [ ] Completar atributos `[Required]`, `[StringLength]`, `[Range]` en DTOs
- [ ] Validar estructura de cabecera (processType, idCarga, fechaCarga, versión)
- [ ] Validar estructura del detalle según processType

#### Paso 4.2: Validación avanzada (opcional con FluentValidation)
- [ ] Crear `WebhookPayloadValidator.cs` con FluentValidation
- [ ] Validar formatos de fecha
- [ ] Validar rangos de versión
- [ ] Validar estructura JSON anidada

#### Paso 4.3: Extracción de campos clave
- [ ] Crear método `ExtractKeyFields()` en `WebhookService`
- [ ] Extraer y validar:
  - `processType`
  - `idCarga`
  - `fechaCarga`
  - `versión`
  - `metadata`
- [ ] Responder `400 Bad Request` si el payload es inválido
- [ ] Registrar errores de validación

---

### **FASE 5: Resolución del Proceso de Negocio (Patrón Estrategia)**

#### Paso 5.1: Crear interfaz ISyncHandler
- [ ] Definir `ISyncHandler` con método `HandleAsync(WebhookPayloadDto payload)`
- [ ] Definir método `CanHandle(string processType)`
- [ ] Definir propiedad `ProcessType`

#### Paso 5.2: Implementar handlers específicos
- [ ] `ProductListSyncHandler.cs` - Para processType "productList"
- [ ] `CampaignListSyncHandler.cs` - Para processType "campaignList"
- [ ] Cada handler implementa lógica de UPSERT específica

#### Paso 5.3: Crear SyncHandlerFactory
- [ ] Crear `ISyncHandlerFactory` y `SyncHandlerFactory`
- [ ] Registrar handlers en DI container
- [ ] Implementar método `GetHandler(string processType)`
- [ ] Responder `422 Unprocessable Entity` si processType no es soportado
- [ ] Registrar eventos no soportados para auditoría

---

### **FASE 6: Validación de Versión y Control de Idempotencia**

#### Paso 6.1: Crear SyncControlRepository
- [ ] Implementar `ISyncControlRepository`
- [ ] Método `ObtenerPorProcesoAsync(string processType, string idCarga, DateTime fechaCarga)`
- [ ] Método `CrearAsync(SyncControl entity)`
- [ ] Método `ActualizarAsync(SyncControl entity)`

#### Paso 6.2: Implementar lógica de validación de versión
- [ ] Crear método `ValidateVersionAsync()` en `WebhookService`
- [ ] Consultar `LOCAL_SYNC_CONTROL` con processType, idCarga, fechaCarga
- [ ] Comparar versión entrante con almacenada:
  - **Versión repetida:** Retornar `true` (idempotencia) - ignorar procesamiento
  - **Versión anterior:** Retornar `false` - rechazar con `409 Conflict`
  - **Versión más nueva:** Retornar `true` - continuar procesamiento
- [ ] Responder `200 OK` inmediatamente si es versión repetida (idempotencia)

#### Paso 6.3: Implementar control de idempotencia
- [ ] Generar `IdempotencyKey` basado en `processType + idCarga + fechaCarga`
- [ ] Verificar si ya existe registro con mismo `IdempotencyKey`
- [ ] Evitar reprocesamiento accidental

---

### **FASE 7: Ejecución de la Sincronización (UPSERTs)**

#### Paso 7.1: Crear repositorios para entidades de negocio
- [ ] `IProductRepository.cs` - Para productos
- [ ] `ProductRepository.cs` - Implementación con Dapper
- [ ] Métodos `UpsertProductAsync()` para INSERT/UPDATE

#### Paso 7.2: Implementar lógica de UPSERT en handlers
- [ ] En `ProductListSyncHandler`:
  - Iniciar transacción de base de datos
  - Procesar JSON del detalle
  - Ejecutar UPSERTs por cada producto
  - Respetar integridad referencial (PK/FK)
  - Commit o Rollback según resultado
- [ ] Manejar errores de base de datos:
  - Capturar excepciones Oracle
  - Hacer ROLLBACK
  - Registrar fallo con detalle técnico
  - Responder `500 Internal Server Error`

#### Paso 7.3: Implementar procesamiento transaccional
- [ ] Usar `IDbConnection.BeginTransaction()` con Dapper
- [ ] Asegurar atomicidad (todo o nada)
- [ ] Manejar timeouts y deadlocks
- [ ] Logging detallado de operaciones

---

### **FASE 8: Registro de Control y Generación de ACK**

#### Paso 8.1: Actualizar LOCAL_SYNC_CONTROL
- [ ] Después de sincronización exitosa:
  - Actualizar `LastProcessedTimestamp`
  - Actualizar `RecordsReceived` y `RecordsProcessed`
  - Actualizar `Status` = "SUCCESS"
  - Generar y guardar `AckToken`

#### Paso 8.2: Generar ACK Token
- [ ] Crear método `GenerateAckTokenAsync()`
- [ ] Generar token único (ej: SHA256 de processType + idCarga + timestamp)
- [ ] Formato: `ACK-{hash}`

#### Paso 8.3: Construir respuesta ACK
- [ ] Crear `WebhookAckResponseDto` con:
  - `status`: "SUCCESS"
  - `processType`
  - `idCarga`
  - `ackToken`
  - `processedAt`: timestamp en hora de México
- [ ] Responder `200 OK` con payload JSON

---

### **FASE 9: Logging, Auditoría y Métricas con Hangfire Dashboard**

#### Paso 9.1: Logging estructurado
- [ ] Configurar Serilog en `Program.cs`
- [ ] Logging en cada paso del proceso:
  - Recepción de webhook
  - Validación de seguridad
  - Validación de payload
  - Procesamiento
  - Resultado final
- [ ] Incluir `CorrelationId` para trazabilidad
- [ ] Logging de errores con stack traces

#### Paso 9.2: Auditoría (tabla LOCAL_SYNC_LOG)
- [ ] Crear `SyncLogRepository`
- [ ] Registrar cada webhook recibido:
  - Request payload completo
  - Response payload
  - HTTP status code
  - Tiempo de procesamiento
  - Errores (si aplica)
- [ ] Opcional: Retención de logs (política de limpieza)

#### Paso 9.3: Métricas y monitoreo con Hangfire Dashboard
- [ ] Configurar Hangfire Dashboard en `/hangfire`:
  - Autenticación para acceso al dashboard
  - Configurar permisos (solo usuarios autorizados)
- [ ] Utilizar métricas nativas de Hangfire:
  - Jobs en ejecución
  - Jobs completados/exitosos/fallidos
  - Tiempo de procesamiento por job
  - Historial de jobs
  - Reintentos automáticos
- [ ] Health Checks para el servicio:
  - Verificar conexión a Redis
  - Verificar conexión a base de datos
  - Verificar estado de Hangfire
- [ ] Endpoint opcional `/health` o `/metrics`
- [ ] Integrar métricas de Hangfire con logging estructurado

---

### **FASE 10: Características Adicionales y Reintentos**

#### Paso 10.1: Configurar reintentos automáticos con Hangfire
- [ ] Configurar `[AutomaticRetry]` en jobs de Hangfire:
  - Número máximo de reintentos (ej: 3)
  - Delay entre reintentos (backoff exponencial)
  - Condiciones para reintentar (solo errores técnicos, no funcionales)
- [ ] Implementar lógica de reintento inteligente:
  - Reintentar solo en errores 5xx o timeouts
  - NO reintentar en errores 4xx (errores funcionales)
  - Registrar cada intento en logs
- [ ] Configurar Dead Letter Queue para jobs que fallan después de todos los reintentos
- [ ] Notificaciones/alertas para jobs que fallan persistentemente

#### Paso 10.2: Rate Limiting
- [ ] Implementar rate limiting por dealer
- [ ] Protección contra reintentos excesivos
- [ ] Evitar saturación del servicio
- [ ] Responder `429 Too Many Requests` si se excede límite

#### Paso 10.3: Idempotency Key adicional
- [ ] Header opcional `X-Idempotency-Key`
- [ ] Validar y almacenar para evitar reprocesamiento
- [ ] Responder `200 OK` con mismo ACK si se repite

#### Paso 10.4: Configuración y Secretos
- [ ] `appsettings.json` para configuración:
  - Connection strings
  - Webhook secrets por dealer
  - JWT settings (si aplica)
  - Rate limiting config
- [ ] Azure Key Vault o AWS Secrets Manager para secretos (opcional)

---

### **FASE 11: Pruebas y Documentación**

#### Paso 11.1: Pruebas unitarias
- [ ] Tests para validadores
- [ ] Tests para handlers
- [ ] Tests para lógica de versionado
- [ ] Tests para generación de ACK

#### Paso 11.2: Pruebas de integración
- [ ] Tests para endpoint completo
- [ ] Tests con base de datos en memoria o test container
- [ ] Tests de idempotencia
- [ ] Tests de manejo de errores

#### Paso 11.3: Documentación
- [ ] Documentación XML completa en todos los controllers
- [ ] README.md con instrucciones de setup
- [ ] Documentación de API en Swagger
- [ ] Diagramas de flujo (opcional)

---

## 🔄 Flujo Completo del Proceso

```
1. Webhook recibido → POST /webhook/actualizacion-proceso
2. Validación de seguridad (X-Webhook-Secret o JWT)
   ├─ ❌ Fallo → 401/403 + Log
   └─ ✅ Éxito → Continuar
3. Validación estructural del payload
   ├─ ❌ Inválido → 400 + Log
   └─ ✅ Válido → Continuar
4. Intentar adquirir Lock Distribuido (Redis RedLock)
   ├─ ❌ Lock ocupado → 409 Conflict - Proceso ya en ejecución
   └─ ✅ Lock adquirido → Continuar
5. Encolar Job en Hangfire (Background)
   └─ ✅ Job encolado → Responder 202 Accepted con jobId
6. [En Background - Hangfire Job]
   a. Resolución del proceso (SyncHandlerFactory)
      ├─ ❌ No soportado → 422 + Log + Liberar Lock
      └─ ✅ Soportado → Continuar
   b. Validación de versión e idempotencia
      ├─ Versión repetida → 200 OK (idempotencia) + Liberar Lock
      ├─ Versión anterior → 409 Conflict + Liberar Lock
      └─ Versión nueva → Continuar
   c. Ejecución de sincronización (UPSERTs)
      ├─ ❌ Error → 500 + Rollback + Log + Reintento automático (Hangfire)
      └─ ✅ Éxito → Continuar
   d. Actualizar LOCAL_SYNC_CONTROL
   e. Generar ACK Token
   f. Liberar Lock Distribuido
   g. Registrar en LOCAL_SYNC_LOG (opcional)
7. Monitoreo en Hangfire Dashboard (/hangfire)
   - Ver estado de jobs
   - Ver historial de ejecuciones
   - Ver reintentos automáticos
   - Ver métricas de performance
```

---

## 🛠️ Tecnologías y Librerías

### Core
- **ASP.NET Core 9.0** - Framework web
- **Dapper** - Micro-ORM para Oracle
- **Oracle.ManagedDataAccess.Core** - Driver Oracle

### Distributed Locking y Background Jobs
- **RedLock.net** - Implementación de distributed locking con Redis (algoritmo RedLock)
- **StackExchange.Redis** - Cliente Redis para RedLock
- **Hangfire.Core** - Framework para ejecución de jobs en background
- **Hangfire.AspNetCore** - Integración de Hangfire con ASP.NET Core
- **Hangfire.SqlServer** o **Hangfire.PostgreSql** - Storage para Hangfire (o usar Redis como storage)

### Logging y Monitoreo
- **Serilog** - Logging estructurado
- **Serilog.Sinks.File** - Logging a archivos
- **Health Checks** - Monitoreo de salud

### Validación
- **Data Annotations** - Validación básica
- **FluentValidation** (opcional) - Validación avanzada

### Seguridad
- **Microsoft.AspNetCore.Authentication.JwtBearer** - Si se usa JWT
- **Azure Key Vault** (opcional) - Gestión de secretos

### Documentación
- **Swashbuckle.AspNetCore** - Swagger
- **Scalar.AspNetCore** - Documentación interactiva

---

## 📊 Consideraciones de Performance

1. **Procesamiento asíncrono:** Todos los métodos deben ser `async/await`
2. **Transacciones:** Usar transacciones explícitas para UPSERTs
3. **Connection Pooling:** Configurar pool de conexiones Oracle
4. **Timeouts:** Configurar timeouts apropiados para operaciones de BD
5. **Batch Processing:** Para grandes volúmenes, procesar en lotes
6. **Redis Performance:**
   - Configurar connection pooling para Redis
   - Usar Redis en modo cluster para alta disponibilidad (opcional)
   - Configurar timeout apropiado para operaciones de lock
7. **Hangfire Performance:**
   - Configurar número de workers según carga esperada
   - Usar múltiples queues para diferentes tipos de jobs
   - Configurar polling interval apropiado
   - Considerar usar Redis como storage para mejor performance (opcional)

---

## 🔒 Seguridad

1. **Autenticación:** Validar X-Webhook-Secret o JWT en cada request
2. **HTTPS:** Forzar HTTPS en producción
3. **Secretos:** No hardcodear secretos, usar configuración segura
4. **Validación de entrada:** Validar y sanitizar todos los inputs
5. **Rate Limiting:** Proteger contra abusos

---

## 📝 Notas de Implementación

1. **Seguir convenciones de .cursorrules:**
   - Nomenclatura: `{Company}.{Module}.{Layer}`
   - Documentación XML obligatoria
   - Uso de Dapper (NO Entity Framework)
   - Structured logging
   - Manejo de errores por capa

2. **Hora de México:**
   - Usar `DateTimeHelper.GetMexicoDateTime()` para timestamps
   - Usar `DateTimeHelper.GetMexicoTimeString()` para strings

3. **Respuestas API:**
   - Usar `ApiResponse<T>` de Shared.Contracts
   - Incluir `Timestamp` en todas las respuestas

4. **Base de datos local:**
   - Este proyecto usa su propia base de datos Oracle local
   - NO debe depender de la BD central (GM.CatalogSync)

5. **Redis RedLock:**
   - Usar para evitar ejecuciones concurrentes del mismo proceso
   - Lock key basado en `processType` (ej: `"sync-lock:productList"`)
   - Configurar expiry time apropiado (ej: 30 minutos)
   - Liberar lock siempre (en finally block o using statement)
   - Manejar casos de lock expirado o liberado prematuramente

6. **Hangfire:**
   - Jobs se ejecutan en background, no bloquean el endpoint
   - Usar `[AutomaticRetry]` para reintentos automáticos
   - Configurar dashboard con autenticación
   - Usar job filters para logging y métricas
   - Considerar usar Redis como storage para mejor escalabilidad

---

## ✅ Checklist de Finalización

- [ ] Todas las fases implementadas
- [ ] Pruebas unitarias y de integración pasando
- [ ] Documentación XML completa
- [ ] Swagger configurado y documentado
- [ ] Logging estructurado funcionando
- [ ] Health checks implementados
- [ ] README.md actualizado
- [ ] Code review completado
- [ ] Despliegue en ambiente de pruebas

---

## 🚀 Próximos Pasos

1. Revisar y aprobar este plan
2. Crear estructura inicial del proyecto
3. Comenzar con Fase 1 (Configuración inicial)
4. Iterar fase por fase con validación continua

---

---

## 🔐 Redis RedLock - Detalles de Implementación

### ¿Por qué RedLock?
- **Distributed Locking:** Evita ejecuciones concurrentes del mismo proceso en entornos distribuidos
- **Algoritmo RedLock:** Implementación robusta que funciona con múltiples instancias de Redis
- **Tolerancia a fallos:** Funciona incluso si algunos nodos de Redis fallan
- **Expiración automática:** Los locks expiran automáticamente para evitar deadlocks

### Configuración Recomendada
```csharp
// En Program.cs
var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
var redLockFactory = RedLockFactory.Create(new List<RedLockEndPoint>
{
    new DnsEndPoint("redis-server", 6379)
});

// Configurar expiry time (30 minutos por defecto)
var lockExpiry = TimeSpan.FromMinutes(30);
```

### Uso en el Código
```csharp
// Adquirir lock
var lockKey = $"sync-lock:{processType}";
using var redLock = await redLockFactory.CreateLockAsync(
    lockKey, 
    lockExpiry,
    retryCount: 3,
    retryDelay: TimeSpan.FromMilliseconds(200)
);

if (redLock.IsAcquired)
{
    // Procesar sincronización
}
else
{
    // Lock no adquirido - proceso ya en ejecución
    return Conflict("Proceso ya en ejecución");
}
```

---

## ⚙️ Hangfire - Detalles de Implementación

### ¿Por qué Hangfire?
- **Background Jobs:** Ejecuta trabajos en segundo plano sin bloquear el endpoint
- **Dashboard Integrado:** Monitoreo visual de jobs, reintentos y métricas
- **Reintentos Automáticos:** Configuración fácil de políticas de reintento
- **Persistencia:** Jobs se almacenan en BD, sobreviven a reinicios
- **Escalabilidad:** Múltiples workers pueden procesar jobs en paralelo

### Configuración Recomendada
```csharp
// En Program.cs
builder.Services.AddHangfire(config =>
{
    config.UseSqlServerStorage(connectionString);
    // O usar Redis: config.UseRedisStorage(redisConnectionString);
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 5;
    options.Queues = new[] { "default", "sync", "retry" };
});

// Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});
```

### Uso en el Código
```csharp
// Encolar job
var jobId = BackgroundJob.Enqueue<IBatchSyncJobService>(
    service => service.ProcessSyncJobAsync(processType, payload));

// Job con reintentos automáticos
[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
public async Task ProcessSyncJobAsync(string processType, WebhookPayloadDto payload)
{
    // Lógica de procesamiento
}
```

### Beneficios Clave
1. **No bloquea el endpoint:** Responde inmediatamente con `202 Accepted`
2. **Reintentos automáticos:** Hangfire maneja reintentos con backoff exponencial
3. **Monitoreo:** Dashboard muestra estado de todos los jobs
4. **Persistencia:** Jobs sobreviven a reinicios del servidor
5. **Escalabilidad:** Múltiples instancias pueden procesar jobs

---

**Fecha de creación:** 2025-01-05  
**Última actualización:** 2025-01-05  
**Versión:** 1.1 (Agregado Redis RedLock y Hangfire)

