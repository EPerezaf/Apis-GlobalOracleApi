-- =====================================================
-- Script SQL: Tabla CO_EVENTOSCARGASINCCONTROL
-- Propósito: Gestionar el control de ejecución de procesos de sincronización batch
-- Relación: Se relaciona con CO_EVENTOSCARGAPROCESO mediante COCP_EVENTOCARGAPROCESOID
-- NOTA: Esta es la estructura FINAL de la tabla creada en Oracle
-- =====================================================

-- Crear tabla para control de sincronización batch
CREATE TABLE CO_EVENTOSCARGASINCCONTROL (
    -- Identificador único
    COES_SINCCONTROLID          NUMBER           NOT NULL,
    
    -- Información del proceso
    COES_PROCESSTYPE            VARCHAR2(50)     NOT NULL,
    COES_IDCARGA                VARCHAR2(100)    NOT NULL,
    COES_FECHACARGA             DATE             NOT NULL,
    
    -- Relación con CO_EVENTOSCARGAPROCESO
    COES_COCP_EVENTPROCESOID    NUMBER,
    
    -- Control de Hangfire
    COES_HANGFIREJOBID          VARCHAR2(100),
    
    -- Estado del proceso
    COES_STATUS                 VARCHAR2(20)     DEFAULT 'PENDING' NOT NULL,
    COES_FECHAINICIO            DATE,
    COES_FECHAFIN               DATE,
    
    -- Estadísticas del proceso
    COES_TOTALDEALERS           NUMBER           DEFAULT 0,
    COES_DEALERSPROCESADOS      NUMBER           DEFAULT 0,
    COES_DEALERSFALLIDOS        NUMBER           DEFAULT 0,
    COES_DEALERSOMITIDOS        NUMBER           DEFAULT 0,
    
    -- Información de error
    COES_ERRORMESSAGE           VARCHAR2(2000),
    COES_ERRORDETAILS           CLOB,
    
    -- Campos de auditoría (sin prefijo)
    FECHAREGISTRO               DATE             DEFAULT SYSDATE NOT NULL,
    USUARIOREGISTRO             VARCHAR2(100),
    FECHAMODIFICACION           DATE,
    USUARIOMODIFICACION         VARCHAR2(100),

    -- Clave primaria
    CONSTRAINT PK_CO_EVENTOSCARGASINCCONTROL PRIMARY KEY (COES_SINCCONTROLID),

    -- NOTA: No se incluye restricción UNIQUE para permitir múltiples ejecuciones del mismo proceso
    -- (permite re-ejecutar procesos que terminaron COMPLETED o FAILED)

    -- Check constraint para status
    CONSTRAINT CHK_COES_STATUS CHECK (COES_STATUS IN ('PENDING', 'RUNNING', 'COMPLETED', 'FAILED')),

    -- Checks de NOT NULL
    CONSTRAINT NN_COES_SINCCONTROLID CHECK (COES_SINCCONTROLID IS NOT NULL),
    CONSTRAINT NN_COES_PROCESSTYPE CHECK (COES_PROCESSTYPE IS NOT NULL),
    CONSTRAINT NN_COES_IDCARGA CHECK (COES_IDCARGA IS NOT NULL),
    CONSTRAINT NN_COES_FECHACARGA CHECK (COES_FECHACARGA IS NOT NULL),
    CONSTRAINT NN_COES_STATUS CHECK (COES_STATUS IS NOT NULL),
    CONSTRAINT NN_FECHAREGISTRO CHECK (FECHAREGISTRO IS NOT NULL)
);

-- Crear secuencia para COES_SINCCONTROLID (si no se usa IDENTITY)
-- CREATE SEQUENCE SEQ_COES_SINCCONTROLID START WITH 1 INCREMENT BY 1;

-- Comentarios en la tabla
COMMENT ON TABLE LOCAL_SYNC_CONTROL IS 'Tabla para gestionar el control de ejecución de procesos de sincronización batch';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_SYNC_CONTROL_ID IS 'Identificador único del registro';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_PROCESS_TYPE IS 'Tipo de proceso (debe coincidir con el enum ProcessType en código)';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_ID_CARGA IS 'ID de la carga del proceso';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_FECHA_CARGA IS 'Fecha de carga del proceso';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_COCP_EVENTOCARGAPROCESOID IS 'FK a CO_EVENTOSCARGAPROCESO.COCP_EVENTOCARGAPROCESOID';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_HANGFIRE_JOB_ID IS 'JobId de Hangfire para tracking del job en background';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_STATUS IS 'Estado del proceso: PENDING, RUNNING, COMPLETED, FAILED';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_FECHA_INICIO IS 'Fecha y hora de inicio del proceso';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_FECHA_FIN IS 'Fecha y hora de finalización del proceso';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_TOTAL_DEALERS IS 'Total de dealers a procesar';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_DEALERS_PROCESADOS IS 'Cantidad de dealers procesados exitosamente';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_DEALERS_FALLIDOS IS 'Cantidad de dealers que fallaron';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_DEALERS_OMITIDOS IS 'Cantidad de dealers omitidos (ya sincronizados)';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_ERROR_MESSAGE IS 'Mensaje de error si el proceso falló';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_ERROR_DETAILS IS 'Detalles adicionales del error (stack trace, etc.)';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_FECHA_ALTA IS 'Fecha y hora de creación del registro';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_USUARIO_ALTA IS 'Usuario que creó el registro';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_FECHA_MODIFICACION IS 'Fecha y hora de última actualización';
COMMENT ON COLUMN LOCAL_SYNC_CONTROL.LSC_USUARIO_MODIFICACION IS 'Último usuario que actualizó el registro';

-- NOTA: No se crea índice único para permitir múltiples ejecuciones del mismo proceso
-- (permite re-ejecutar procesos que terminaron COMPLETED o FAILED)
-- Si necesitas un índice no único para búsquedas rápidas, puedes crearlo así:
-- CREATE INDEX IDX_COES_PROCESS_SEARCH 
-- ON CO_EVENTOSCARGASINCCONTROL(COES_PROCESSTYPE, COES_IDCARGA, COES_FECHACARGA);

-- NOTA: El siguiente índice está comentado porque usa nombres de columnas diferentes
-- CREATE UNIQUE INDEX IDX_LSC_PROCESS_UNIQUE 
-- ON LOCAL_SYNC_CONTROL(LSC_PROCESS_TYPE, LSC_ID_CARGA, LSC_FECHA_CARGA);

-- Crear índice para búsqueda por status
CREATE INDEX IDX_LSC_STATUS 
ON LOCAL_SYNC_CONTROL(LSC_STATUS);

-- Crear índice para búsqueda por Hangfire JobId
CREATE INDEX IDX_LSC_HANGFIRE_JOB_ID 
ON LOCAL_SYNC_CONTROL(LSC_HANGFIRE_JOB_ID);

-- Crear índice para relación con CO_EVENTOSCARGAPROCESO
CREATE INDEX IDX_LSC_COCP_EVENTOCARGAPROCESOID 
ON LOCAL_SYNC_CONTROL(LSC_COCP_EVENTOCARGAPROCESOID);

-- Crear foreign key a CO_EVENTOSCARGAPROCESO (si la tabla existe)
-- ALTER TABLE LOCAL_SYNC_CONTROL
-- ADD CONSTRAINT FK_LSC_COCP_EVENTOCARGAPROCESOID 
-- FOREIGN KEY (LSC_COCP_EVENTOCARGAPROCESOID) 
-- REFERENCES CO_EVENTOSCARGAPROCESO(COCP_EVENTOCARGAPROCESOID);

-- =====================================================
-- Ejemplos de consultas útiles
-- =====================================================

-- Obtener procesos pendientes (Paso 12 del diagrama)
-- SELECT * FROM LOCAL_SYNC_CONTROL 
-- WHERE LSC_STATUS = 'PENDING' 
-- ORDER BY LSC_FECHA_ALTA ASC;

-- Obtener procesos en ejecución
-- SELECT * FROM LOCAL_SYNC_CONTROL 
-- WHERE LSC_STATUS = 'RUNNING' 
-- ORDER BY LSC_FECHA_INICIO DESC;

-- Obtener proceso por ProcessType e IdCarga
-- SELECT * FROM LOCAL_SYNC_CONTROL 
-- WHERE LSC_PROCESS_TYPE = 'ProductList' 
--   AND LSC_ID_CARGA = 'Carga0001';

-- Actualizar status a RUNNING (Paso 9 del diagrama)
-- UPDATE LOCAL_SYNC_CONTROL 
-- SET LSC_STATUS = 'RUNNING',
--     LSC_HANGFIRE_JOB_ID = 'job-id-123',
--     LSC_FECHA_INICIO = SYSDATE,
--     LSC_FECHA_MODIFICACION = SYSDATE,
--     LSC_USUARIO_MODIFICACION = 'SYSTEM'
-- WHERE LSC_SYNC_CONTROL_ID = 1;

-- Actualizar status a COMPLETED (al finalizar)
-- UPDATE LOCAL_SYNC_CONTROL 
-- SET LSC_STATUS = 'COMPLETED',
--     LSC_FECHA_FIN = SYSDATE,
--     LSC_DEALERS_PROCESADOS = 10,
--     LSC_FECHA_MODIFICACION = SYSDATE,
--     LSC_USUARIO_MODIFICACION = 'SYSTEM'
-- WHERE LSC_SYNC_CONTROL_ID = 1;

-- Actualizar status a FAILED (si falla)
-- UPDATE LOCAL_SYNC_CONTROL 
-- SET LSC_STATUS = 'FAILED',
--     LSC_FECHA_FIN = SYSDATE,
--     LSC_ERROR_MESSAGE = 'Error al procesar dealers',
--     LSC_FECHA_MODIFICACION = SYSDATE,
--     LSC_USUARIO_MODIFICACION = 'SYSTEM'
-- WHERE LSC_SYNC_CONTROL_ID = 1;

