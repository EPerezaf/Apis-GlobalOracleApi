using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.CatalogSync.Infrastructure.Repositories;

public class CampañaRepository: ICampañaRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<CampañaRepository> _logger;

    public CampañaRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<CampañaRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;    
    }

    public async Task<(List<Campaña> campañas, int totalRecords)> GetByFilterAsync(
        string? sourceCodeId,
        string? id,
        string? name,
        int page,
        int pageSize,
        string correlationId)
    {
        try
        {
            _logger.LogInformation("[{CorrelationId}] [REPOSITORY] Consultando campañas - SourceCodeId: {SourceCodeId}, Id: {Id}, Name: {Name}",
            correlationId, sourceCodeId ?? "Todos", id ?? "Todos", name ?? "Todos", page);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(sourceCodeId))
            {
                whereClause += " AND COCA_SOURCECODEID = :sourceCodeId";
                parameters.Add("sourceCodeId", sourceCodeId);
            }
            if (!string.IsNullOrWhiteSpace(id))
            {
                whereClause += " AND COCA_ID = : id";
                parameters.Add("id", id);
            }
            if (!string.IsNullOrWhiteSpace(name))
            {
                whereClause += " AND COCA_NAME = :name";
                parameters.Add("name", name);
            }

            //OBTENER TOTAL DE REGISTROS
            var countSql = $"SELECT COUNT(*) FROM LABGDMS.CO_GM_LISTADOCAMPAÑAS {whereClause}";
            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            if(totalRecords == 0)
            {
                return (new List<Campaña>(), 0);
            }

            int offset = (page - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", offset + pageSize);

            var sql = $@"
                SELECT * FROM (
                    SELECT 
                        COCA_CAMPAÑAID as CampañaId,
                        COCA_SOURCECODEID as SourceCodeId,
                        COCA_ID as Id,
                        COCA_NAME as Name,
                        COCA_RECORDTYPEID as RecordTypeId,
                        COCA_LEADRECORDTYPE as LeadRecordType,
                        COCA_LEADENQUIRYTYPE as LeadEnquiryType,
                        COCA_LEADSOURCE as LeadSource,
                        COCA_LEADSOURCEDETAILS as LeadSourceDetails,
                        COCA_STATUS as Status,
                        FECHAALTA as FechaAlta,
                        USUARIOALTA as UsuarioAlta,
                        FECHAMODIFICACION as FechaModificacion,
                        USUARIOMODIFICACION as UsuarioModificacion,
                        ROW_NUMBER() OVER (ORDER BY COCA_CAMPAÑAID) AS RNUM
                    FROM LABGDMS.CO_GM_LISTADOCAMPAÑAS
                    {whereClause}
                    ) WHERE RNUM > :offset AND RNUM <= :limit";

            var campañas = await connection.QueryAsync<Campaña>(sql, parameters);

            _logger.LogInformation("[{CorrelationId}] [REPOSITORY] Consulta completa - {Count} registros de {Total} totales",
            correlationId, campañas.Count(), totalRecords);

            return (campañas.ToList(), totalRecords);
        }    
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error Oracle en GetByFilterAsync", correlationId);
            throw new DataAccessException("Error al consultar campañas", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error inesperado en GetByFilterAsync", correlationId);
            throw new DataAccessException("Error inesperado al consultar campañas", ex);
        }
    }

    public async Task<int> GetTotalCountAsync(
        string? sourceCodeId,
        string? id,
        string? name,
        string correlationId)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(sourceCodeId))
            {
                whereClause += " AND COCA_SOURCECODEID = :sourceCodeId";
                parameters.Add("sourceCodeId", sourceCodeId);
            }
            if (!string.IsNullOrWhiteSpace(id))
            {
                whereClause += " AND COCA_ID = : id";
                parameters.Add("id", id);
            }
            if (!string.IsNullOrWhiteSpace(name))
            {
                whereClause += " AND COCA_NAME = :name";
                parameters.Add("name", name);
            }

            var sql = $"SELECT COUNT(*) FROM LABGDMS.CO_GM_LISTADOCAMPAÑAS {whereClause}";
            var count = await connection.ExecuteScalarAsync<int>(sql, parameters);

            return count;
        }    
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error Oracle en GetTotalCountAsync", correlationId);
            throw new DataAccessException("Error al contar campañas",ex);
        }
        catch (Exception ex)
        {
            _logger.LogError("[{CorrelationId}] [REPOSITORY] Error inesperado en GetTotalCountAsync", correlationId);
            throw new DataAccessException("Errro inesperado al contar las campañas",ex);
        }
    }
    
    public async Task<int> InsertAsync(
        Campaña campaña,
        string currentUser,
        string correlationId)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                INSERT INTO CO_GM_LISTADOCAMPAÑAS (
                    COCA_SOURCECODEID, COCA_ID, COCA_NAME,
                    COCA_RECORDTYPEID, COCA_LEADRECORDTYPE,
                    COCA_LEADENQUIRYTYPE, COCA_LEADSOURCE,
                    COCA_LEADSOURCEDETAILS, COCA_STATUS,
                    FECHAALTA, USUARIOALTA 
                    ) VALUES (
                        :sourceCodeId, :id, :name,
                        :recordTypeId, :leadRecordType, :leadEnquiryType,
                        :leadSource, :leadSourceDetails, :status, SYSDATE, :usuarioAlta
                    )";
            var parameters = new DynamicParameters();
            parameters.Add("sourceCodeId", campaña.SourceCodeId);
            parameters.Add("id", campaña.Id);
            parameters.Add("name", campaña.Name);
            parameters.Add("recordTypeId", campaña.RecordTypeId);
            parameters.Add("leadRecordType", campaña.LeadRecordType);
            parameters.Add("leadEnquiryType", campaña.LeadEnquiryType ?? (object)DBNull.Value);
            parameters.Add("leadSource", campaña.LeadSource ?? (object)DBNull.Value);
            parameters.Add("leadSourceDetails", campaña.LeadSourceDetails ?? (object)DBNull.Value);
            parameters.Add("status", campaña.Status ?? (object)DBNull.Value);
            parameters.Add("usuarioAlta", currentUser ?? "SYSTEM");

            var rowsAffected = await connection.ExecuteAsync(sql, parameters);
            return rowsAffected;
        }    
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error Oracle en InsertAsync", correlationId);
            throw new DataAccessException("Error al insertar campaña", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error inesperado en InsertAsync", correlationId);
            throw new DataAccessException("Error inesperado al insertar campañas",ex);
        }
    }

    public async Task<int> UpsertBatchWithTransactionAsync(
        IEnumerable<Campaña> campañas,
        string currentUser,
        string correlationId)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            
            // Verificar si la conexión ya está abierta (connection pool)
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var transaction = connection.BeginTransaction();

            try
            {
                 var sql = @"
                INSERT INTO LABGDMS.CO_GM_LISTADOCAMPAÑAS (
                    COCA_SOURCECODEID, COCA_ID, COCA_NAME,
                    COCA_RECORDTYPEID, COCA_LEADRECORDTYPE,
                    COCA_LEADENQUIRYTYPE, COCA_LEADSOURCE,
                    COCA_LEADSOURCEDETAILS, COCA_STATUS,
                    FECHAALTA, USUARIOALTA 
                    ) VALUES (
                        :sourceCodeId, :id, :name,
                        :recordTypeId, :leadRecordType, :leadEnquiryType,
                        :leadSource, :leadSourceDetails, :status, SYSDATE, :usuarioAlta
                    )";

                int totalInserted = 0;
                foreach (var campaña in campañas)
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("sourceCodeId", campaña.SourceCodeId);
                    parameters.Add("id", campaña.Id);
                    parameters.Add("name", campaña.Name);
                    parameters.Add("recordTypeId", campaña.RecordTypeId);
                    parameters.Add("leadRecordType", campaña.LeadRecordType);
                    parameters.Add("leadEnquiryType", campaña.LeadEnquiryType);
                    parameters.Add("leadSource", campaña.LeadSource);
                    parameters.Add("leadSourceDetails", campaña.LeadSourceDetails);
                    parameters.Add("status", campaña.Status);
                    parameters.Add("usuarioAlta", currentUser ?? "SYSTEM");

                    await connection.ExecuteAsync(sql, parameters, transaction);
                    totalInserted++;
                }

                transaction.Commit();
                _logger.LogInformation("[{CorrelationId}] ✅ [REPOSITORY] Batch INSERT completado - {Count} registros insertados",
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
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error Oracle en UpsertBatchWithTransactionAsync", correlationId);
            throw new DataAccessException("Error al insertar campañas en lote", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error inesperado en UpsertBatchWithTransactionAsync", correlationId);
            throw new DataAccessException("Error inesperado al insertar campañas en lote", ex);
        }
    }

    public async Task<int> DeleteAllAsync(
        string currentUser,
        string correlationId)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = "DELETE FROM LABGDMS.CO_GM_LISTADOCAMPAÑAS";
            var rowsAffected = await connection.ExecuteAsync(sql);

            _logger.LogInformation("[{CorrelationId}] ✅ [REPOSITORY] DELETE completado - {Rows} filas eliminadas",
                correlationId, rowsAffected);

            return rowsAffected;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error Oracle en DeleteAllAsync", correlationId);
            throw new DataAccessException("Error al eliminar campañas", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error inesperado en DeleteAllAsync", correlationId);
            throw new DataAccessException("Error inesperado al eliminar campañas", ex);
        }
    }
}