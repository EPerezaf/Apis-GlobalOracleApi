-- =====================================================
-- Script SQL: Eliminar restricción UNIQUE de CO_EVENTOSCARGASINCCONTROL
-- Propósito: Permitir múltiples registros con los mismos ProcessType, IdCarga y FechaCarga
-- Fecha: 2026-01-06
-- =====================================================

-- Eliminar los índices únicos primero (si existen - verificar ambos nombres posibles)
BEGIN
    BEGIN
        EXECUTE IMMEDIATE 'DROP INDEX IDX_COES_PROCESS_UNIQUE';
        DBMS_OUTPUT.PUT_LINE('Índice IDX_COES_PROCESS_UNIQUE eliminado');
    EXCEPTION
        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('Índice IDX_COES_PROCESS_UNIQUE no existe (ok)');
    END;
    
    BEGIN
        EXECUTE IMMEDIATE 'DROP INDEX IDX_LSC_PROCESS_UNIQUE';
        DBMS_OUTPUT.PUT_LINE('Índice IDX_LSC_PROCESS_UNIQUE eliminado');
    EXCEPTION
        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('Índice IDX_LSC_PROCESS_UNIQUE no existe (ok)');
    END;
END;
/

-- Eliminar la restricción UNIQUE (si existe)
BEGIN
    BEGIN
        EXECUTE IMMEDIATE 'ALTER TABLE CO_EVENTOSCARGASINCCONTROL DROP CONSTRAINT UQ_COES_PROCESS_UNIQUE';
        DBMS_OUTPUT.PUT_LINE('Restricción UQ_COES_PROCESS_UNIQUE eliminada');
    EXCEPTION
        WHEN OTHERS THEN
            DBMS_OUTPUT.PUT_LINE('Restricción UQ_COES_PROCESS_UNIQUE no existe (ok)');
    END;
END;
/

-- Verificar que se eliminó correctamente
-- SELECT * FROM USER_CONSTRAINTS WHERE TABLE_NAME = 'CO_EVENTOSCARGASINCCONTROL' AND CONSTRAINT_TYPE = 'U';
-- SELECT * FROM USER_INDEXES WHERE TABLE_NAME = 'CO_EVENTOSCARGASINCCONTROL' AND INDEX_NAME LIKE '%UNIQUE%';

-- =====================================================
-- NOTA: Ahora se pueden crear múltiples registros con los mismos 
-- ProcessType, IdCarga y FechaCarga, lo que permite re-ejecutar 
-- procesos que terminaron (COMPLETED o FAILED)
-- =====================================================

