using System.Linq.Expressions;
using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.CatalogSync.Infrastructure.Repositories;

public class AsignacionRepository : IAsignacionRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<AsignacionRepository> _logger;

    public AsignacionRepository( 
        IOracleConnectionFactory connectionFactory,
        ILogger<AsignacionRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<(List<Asignacion> asignacion, int totalRecords)> GetByFilterAsync(
        string? usuario,
        string? dealer,
        int page,
        int pageSize,
        string correlationId)
    {
        try
        {
            _logger.LogInformation("[{CorrrelationId}] [REPOSITORY] Consultando asignaciones - Usuario: {Usuario}, Dealer: {Dealer}, Pagina: {Page}",
            correlationId, usuario ?? "Todos", dealer ?? "Todos", page);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(usuario))
            {
                whereClause += " AND COUD_USUARIO = :usuario";
                parameters.Add("usuario", usuario);
            }

            if (!string.IsNullOrWhiteSpace(dealer))
            {
                whereClause += " AND COUD_DEALER = :dealer";
                parameters.Add("dealer", dealer);
            }

            //OBTENER TOTAL DE REGISTROS
            var countSql = $"SELECT COUNT(*) FROM LABGDMS.CO_USUARIOXDEALER {whereClause}";
            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql,parameters);

            if(totalRecords == 0)
            {
                return (new List<Asignacion>(), 0);
            }

            int offset = (page - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", offset + pageSize);

            var sql = $@"
                SELECT * FROM (
                    SELECT 
                        COUD_USUARIO as Usuario,
                        COUD_DEALER as Dealer,
                        USUARIOALTA as UsuarioAlta,
                        FECHAALTA as FechaAlta,
                        USUARIOMODIFICA as UsuarioModificacion,
                        FECHAMODIFICA as FechaModificacion,
                        ROW_NUMBER() OVER (ORDER BY COUD_USUARIO) AS RNUM
                    FROM LABGDMS.CO_USUARIOXDEALER
                    {whereClause}
                ) WHERE RNUM > :offset AND RNUM <= :limit";
            var asignacion = await connection.QueryAsync<Asignacion>(sql, parameters);

            _logger.LogInformation("[{CorrealtionId}] [REPOSITORY] Consultando completada - {Count} registros de {Total} totaltes",
            correlationId, asignacion.Count(), totalRecords);

            return (asignacion.ToList(), totalRecords);
        }    
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error Oracle en GetByFilterAsync", correlationId);
            throw new DataAccessException("Error al consultar asignaciones, ex");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error inesperado en getByFilterAsync", correlationId);
            throw new DataAccessException("Error inesperado al consultar asignaciones", ex);
        }
    }

    public async Task<int> GetTotalCountAsync(
        string? usuario,
        string? dealer,
        string correlationId)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(usuario))
            {
                whereClause += " AND COUD_USUARIO = :usuario";
                parameters.Add("usuario", usuario);
            }

            if (!string.IsNullOrWhiteSpace(dealer))
            {
                whereClause += " AND COUD_DEALER = :dealer";
                parameters.Add("dealer", dealer);
            }

            var sql = $"SELECT COUNT(*) FROM LABGDMS.CO_USUARIOXDEALER {whereClause}";
            var count = await connection.ExecuteScalarAsync<int>(sql, parameters);

            return count;
        }    
        catch(OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error oracle en GetTotalCountAsync", correlationId);
            throw new DataAccessException("Error al contar asignaciones", ex);
        }
        catch(Exception ex)
        {
            _logger.LogError("[{CorrelationId}] [REPOSITORY] Error inesperado en GetTotalCountAsync", correlationId);
            throw new DataAccessException("Error inesperado al contar asignaciones", ex);
        }
    }

    public async Task<int>InsertAsync(
        Asignacion asignacion,
        string currentUser,
        string correlationId)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                INSERT INTO CO_USUARIOXDEALER (
                    COUD_USUARIO,
                    COUD_DEALER,
                    USUARIOALTA,
                    FECHALATA,
                    USUARIOMODIFICACION, 
                    FECHAMODIFICACION,
                    ) VALUES (
                        :usuario, :dealer, 
                        SYSDATE, :usuarioAlta
                        SYSDATE, :usuarioModificacion,
                    )";  
            var parameters = new DynamicParameters();
            parameters.Add("usuario", asignacion.Usuario);
            parameters.Add("dealer", asignacion.Dealer);
            parameters.Add("usuarioAlta", currentUser ?? "SYSTEM");
            parameters.Add("usuarioModificacion", currentUser ?? "SYSTEM");

            var rowsAffected = await connection.ExecuteAsync(sql, parameters);
            return rowsAffected;
        }
        catch(OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error inesperado en InsertAsync", correlationId);
            throw new DataAccessException("Error al insertar asignacion(es)", ex);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error inesperado en InsertAsync", correlationId);
            throw new DataAccessException("Error inesperado al insertar asignaciones", ex);
        }
        
    }

    public async Task<int> UpsertBatchWithTransactionAsync(
        IEnumerable<Asignacion> asignaciones,
        string currentUser,
        string correlationId)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            //VERIFICAR SI LA CONEXION YA ESTA ABIERTA (CONNECTION POOL)
            if(connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var transaction = connection.BeginTransaction();

            try
            {
                var sql = @"
                    INSERT INTO LABGDMS.CO_USUARIOXDEALER (
                        COUD_USUARIO, 
                        COUD_DEALER,
                        FECHAALTA,
                        USUARIOALTA,
                        FECHAMODIFICA,
                        USUARIOMODIFICA
                    ) VALUES (
                        :usuario, :dealer,
                        SYSDATE, :usuarioAlta,
                        SYSDATE, :usuarioModificacion
                    )";

                var sqlBitacora = @"
                    INSERT INTO LABGDMS.CO_USUARIOXDEALER_HIST (
                        COUD_USUARIO,
                        COUD_DEALER,
                        COUD_TIPOEVENTO,
                        USUARIOALTA,
                        FECHAALTA,
                        EMPR_EMPRESAID
                    ) VALUES (
                        :usuario, :dealer,
                        :tipoEvento,
                        :usuarioAlta, SYSDATE,
                        :empresaId
                    )";

                int totalInserted = 0;
                foreach (var asignacion in asignaciones)
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("usuario", asignacion.Usuario);
                    parameters.Add("dealer", asignacion.Dealer);
                    parameters.Add("usuarioAlta", currentUser ?? "SYSTEM");
                    parameters.Add("usuarioModificacion", currentUser ?? "SYSTEM");

                    await connection.ExecuteAsync(sql, parameters, transaction);

                    var bitacoraParameters = new DynamicParameters();
                    bitacoraParameters.Add("usuario", asignacion.Usuario);
                    bitacoraParameters.Add("dealer", asignacion.Dealer);
                    bitacoraParameters.Add("empresaId", "2");
                    bitacoraParameters.Add("usuarioAlta", currentUser ?? "SYSTEM");
                    bitacoraParameters.Add("tipoEvento", "ALTA");

                    await connection.ExecuteAsync(sqlBitacora, bitacoraParameters, transaction);

                    totalInserted++;
                }

                transaction.Commit();
                _logger.LogInformation("[{CorrelationId}] [REPOSITORY] Batch Insert compleatdo - {Count} registros insertados",
                correlationId, totalInserted);

                return totalInserted;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            
        }    
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error Oracle en UpsertBatchWithTransactionAsync", correlationId);
            throw new DataAccessException("Error al insertar asignaciones al lote", ex);
        }
       catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error inesperado en UpsertBatchWithTransactionAsync", correlationId);
            throw new DataAccessException("Error inesperado al insertar asignaciones en el lote", ex);
        }
    }
    
    public async Task<int> DeleteAllAsync(
    string usuario,
    string dealer,
    string currentUser, 
    string correlationId)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var transaction = connection.BeginTransaction();
            try
            {
                // 1. Obtener los registros a eliminar
                var selectSql = @"
                    SELECT COUD_USUARIO, COUD_DEALER, EMPR_EMPRESAID
                    FROM LABGDMS.CO_USUARIOXDEALER
                    WHERE 1=1
                    " + (string.IsNullOrWhiteSpace(usuario) ? "" : " AND COUD_USUARIO = :usuario") +
                    (string.IsNullOrWhiteSpace(dealer) ? "" : " AND COUD_DEALER = :dealer");

                var selectParams = new DynamicParameters();
                if (!string.IsNullOrWhiteSpace(usuario))
                    selectParams.Add("usuario", usuario);
                if (!string.IsNullOrWhiteSpace(dealer))
                    selectParams.Add("dealer", dealer);

                var registros = (await connection.QueryAsync<dynamic>(selectSql, selectParams, transaction)).ToList();

                // 2. Insertar en la bitácora antes de eliminar
                var sqlBitacora = @"
                    INSERT INTO LABGDMS.CO_USUARIOXDEALER_HIST (
                        COUD_USUARIO,
                        COUD_DEALER,
                        USUARIOALTA,
                        FECHAALTA,
                        EMPR_EMPRESAID,
                        COUD_TIPOEVENTO
                    ) VALUES (
                        :usuario, :dealer,
                        :usuarioAlta, SYSDATE,
                        :empresaId, :tipoEvento
                    )";

                foreach (var reg in registros)
                {
                    var bitacoraParameters = new DynamicParameters();
                    bitacoraParameters.Add("usuario", reg.COUD_USUARIO);
                    bitacoraParameters.Add("dealer", reg.COUD_DEALER);
                    bitacoraParameters.Add("empresaId", "2");
                    bitacoraParameters.Add("usuarioAlta", currentUser ?? "SYSTEM");
                    bitacoraParameters.Add("tipoEvento", "BAJA");

                    await connection.ExecuteAsync(sqlBitacora, bitacoraParameters, transaction);
                }

                // 3. Eliminar los registros
                var whereClause = "WHERE 1=1";
                var deleteParams = new DynamicParameters();
                if (!string.IsNullOrWhiteSpace(usuario))
                {
                    whereClause += " AND COUD_USUARIO = :usuario";
                    deleteParams.Add("usuario", usuario);
                }
                if (!string.IsNullOrWhiteSpace(dealer))
                {
                    whereClause += " AND COUD_DEALER = :dealer";
                    deleteParams.Add("dealer", dealer);
                }

                var deleteSql = $"DELETE FROM LABGDMS.CO_USUARIOXDEALER {whereClause}";
                var rowsAffected = await connection.ExecuteAsync(deleteSql, deleteParams, transaction);

                transaction.Commit();

                _logger.LogInformation("[{CorrelationId}] [REPOSITORY] DELETE completado - {Rows} filas eliminadas y registradas en bitácora",
                    correlationId, rowsAffected);

                return rowsAffected;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error Oracle en DeleteAllAsync", correlationId);
            throw new DataAccessException("Error al eliminar asignacion", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error inesperado en DeleteAllAsync", correlationId);
            throw new DataAccessException("Error inesperado al eliminar asignacion", ex);
        }
    }
    
    
    
    
    
    
}