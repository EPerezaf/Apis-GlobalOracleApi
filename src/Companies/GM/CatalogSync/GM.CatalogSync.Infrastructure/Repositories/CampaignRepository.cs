using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GM.CatalogSync.Infrastructure.Repositories;

/// <summary>
/// Repository para acceso a datos de Campaign usando Dapper
/// </summary>
public class CampaignRepository : ICampaignRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<CampaignRepository> _logger;

    public CampaignRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<CampaignRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<(List<Campaign> campaigns, int totalRecords)> GetByFiltersAsync(
        string? id,
        string? leadRecordType,
        int page,
        int pageSize,
        string correlationId)
    {
        try
        {
            _logger.LogInformation("[{CorrelationId}] 🗄️ [REPOSITORY] Consultando campanias - Id: {Id}, LeadRecordType: {LeadRecordType}, Página: {Page}",
                 correlationId, id ?? "Todos", leadRecordType ?? "Todos", page);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(id))
            {
                whereClause += " AND COCC_ID = :id";
                parameters.Add("id", id);
            }

            if (!string.IsNullOrWhiteSpace(leadRecordType))
            {
                whereClause += " AND COCC_LEADRECORDTYPE = :leadRecordType";
                parameters.Add("leadRecordType", leadRecordType);
            }

            // Obtener total de registros
            var countSql = $"SELECT COUNT(*) FROM AUTOS.CO_GM_CAMPAIGNCATALOG {whereClause}";
            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            if (totalRecords == 0)
            {
                return (new List<Campaign>(), 0);
            }

            int offset = (page - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", offset + pageSize);

            var sql = $@"
                SELECT * FROM (
                    SELECT 
                        COCC_CAMPANIAID as CampaniaId,
                        COCC_SOURCECODEID as SourceCodeId,
                        COCC_ID as Id,
                        COCC_NAME as Name,
                        COCC_RECORDTYPEID as RecordTypeId,
                        COCC_LEADRECORDTYPE as LeadRecordType,
                        COCC_LEADENQUIRYTYPE as LeadEnquiryType,
                        COCC_LEADSOURCE as LeadSource,
                        COCC_LEADSOURCEDETAILS as LeadSourceDetails,
                        COCC_STATUS as Status,
                        FECHAALTA as FechaAlta,
                        USUARIOALTA as UsuarioAlta,
                        FECHAMODIFICACION as FechaModificacion,
                        USUARIOMODIFICACION as UsuarioModificacion,
                        ROW_NUMBER() OVER (ORDER BY COCC_CAMPANIAID) AS RNUM
                    FROM AUTOS.CO_GM_CAMPAIGNCATALOG
                    {whereClause}

                ) WHERE RNUM > :offset AND RNUM <= :limit";

            var campaigns = await connection.QueryAsync<Campaign>(sql, parameters);

            _logger.LogInformation("[{CorrelationId}] ✅ [REPOSITORY] Consulta completada - {Count} registros de {Total} totales",
                correlationId, campaigns.Count(), totalRecords);

            return (campaigns.ToList(), totalRecords);
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error Oracle en GetByFiltersAsync", correlationId);
            throw new DataAccessException("Error al consultar campanias", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error inesperado en GetByFiltersAsync", correlationId);
            throw new DataAccessException("Error inesperado al consultar campanias", ex);
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
                whereClause += " AND COCC_SOURCECODEID = :sourceCodeId";
                parameters.Add("sourceCodeId", sourceCodeId);
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                whereClause += " AND COCC_ID = :id";
                parameters.Add("id", id);
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                whereClause += " AND COCC_NAME = :name";
                parameters.Add("name", name);
            }

            var sql = $"SELECT COUNT(*) FROM AUTOS.CO_GM_CAMPAIGNCATALOG {whereClause}";
            var count = await connection.ExecuteScalarAsync<int>(sql, parameters);

            return count;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error Oracle en GetTotalCountAsync", correlationId);
            throw new DataAccessException("Error al contar campanias", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error inesperado en GetTotalCountAsync", correlationId);
            throw new DataAccessException("Error inesperado al contar campanias", ex);
        }
    }

    public async Task<bool> ExistsByIdAsync(
        string id,
        string correlationId)
    {
        try
        {
            var idNormalized = (id ?? string.Empty).Trim();

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                SELECT COUNT(*) 
                FROM AUTOS.CO_GM_CAMPAIGNCATALOG 
                WHERE COCC_ID = :id";

            var parameters = new DynamicParameters();
            parameters.Add("id", idNormalized);

            var count = await connection.ExecuteScalarAsync<int>(sql, parameters);
            return count > 0;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error Oracle en ExistByIdAsync", correlationId);
            throw new DataAccessException("Error al verificar existencia", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error inesperado en ExistByIdAsync", correlationId);
            throw new DataAccessException("Error inesperado al verificar existencia", ex);
        }
    }

    public async Task<int> InsertAsync(
        Campaign campaign,
        string currentUser,
        string correlationId)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                INSERT INTO AUTOS.CO_GM_CAMPAIGNCATALOG (
                    COCC_SOURCECODEID, COCC_ID, COCC_NAME, COCC_RECORDTYPEID,
                    COCC_LEADRECORDTYPE, COCC_LEADENQUIRYTYPE, COCC_LEADSOURCE, COCC_LEADSOURCEDETAILS, COCC_STATUS,
                    FECHAALTA, USUARIOALTA, FECHAMODIFICACION, USUARIOMODIFICACION
                ) VALUES (
                    :sourceCodeId, :id, :name, :recordTypeId,
                    :leadRecordType, :leadEnquiryType, :leadSource, :leadSourceDetails, :status,
                    SYSDATE, :usuarioAlta, SYSDATE, :usuarioModificacion
                )";

            var parameters = new DynamicParameters();
            parameters.Add("sourceCodeId", campaign.SourceCodeId);
            parameters.Add("id", campaign.Id);
            parameters.Add("name", campaign.Name);
            parameters.Add("recordTypeId", campaign.RecordTypeId);
            parameters.Add("leadRecordType", campaign.LeadRecordType);
            parameters.Add("leadEnquiryType", campaign.LeadEnquiryType);
            parameters.Add("leadSource", campaign.LeadSource);
            parameters.Add("leadSourceDetails", campaign.LeadSourceDetails);
            parameters.Add("status", campaign.Status);
            parameters.Add("usuarioAlta", currentUser ?? "SYSTEM");
            parameters.Add("usuarioModificacion", currentUser ?? "SYSTEM");

            var rowsAffected = await connection.ExecuteAsync(sql, parameters);
            return rowsAffected;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error Oracle en InsertAsync", correlationId);
            throw new DataAccessException("Error al insertar campania", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error inesperado en InsertAsync", correlationId);
            throw new DataAccessException("Error inesperado al insertar campania", ex);
        }
    }

    public async Task<int> UpsertBatchWithTransactionAsync(
        IEnumerable<Campaign> campaigns,
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
                    INSERT INTO AUTOS.CO_GM_CAMPAIGNCATALOG (
                        COCC_SOURCECODEID, COCC_ID, COCC_NAME, COCC_RECORDTYPEID,
                        COCC_LEADRECORDTYPE, COCC_LEADENQUIRYTYPE, COCC_LEADSOURCE, COCC_LEADSOURCEDETAILS, COCC_STATUS,
                        FECHAALTA, USUARIOALTA, FECHAMODIFICACION, USUARIOMODIFICACION
                    ) VALUES (
                        :sourceCodeId, :id, :name, :recordTypeId,
                    :leadRecordType, :leadEnquiryType, :leadSource, :leadSourceDetails, :status,
                    SYSDATE, :usuarioAlta, SYSDATE, :usuarioModificacion
                    )";

                int totalInserted = 0;
                foreach (var campaign in campaigns)
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("sourceCodeId", campaign.SourceCodeId);
                    parameters.Add("id", campaign.Id);
                    parameters.Add("name", campaign.Name);
                    parameters.Add("recordTypeId", campaign.RecordTypeId);
                    parameters.Add("leadRecordType", campaign.LeadRecordType);
                    parameters.Add("leadEnquiryType", campaign.LeadEnquiryType);
                    parameters.Add("leadSource", campaign.LeadSource);
                    parameters.Add("leadSourceDetails", campaign.LeadSourceDetails);
                    parameters.Add("status", campaign.Status);
                    parameters.Add("usuarioAlta", currentUser ?? "SYSTEM");
                    parameters.Add("usuarioModificacion", currentUser ?? "SYSTEM");
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
            throw new DataAccessException("Error al insertar campanias en lote", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error inesperado en UpsertBatchWithTransactionAsync", correlationId);
            throw new DataAccessException("Error inesperado al insertar campanias en lote", ex);
        }
    }

    public async Task<int> DeleteAllAsync(
        string currentUser,
        string correlationId)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = "DELETE FROM AUTOS.CO_GM_CAMPAIGNCATALOG";
            var rowsAffected = await connection.ExecuteAsync(sql);

            _logger.LogInformation("[{CorrelationId}] ✅ [REPOSITORY] DELETE completado - {Rows} filas eliminadas",
                correlationId, rowsAffected);

            return rowsAffected;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error Oracle en DeleteAllAsync", correlationId);
            throw new DataAccessException("Error al eliminar campanias", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error inesperado en DeleteAllAsync", correlationId);
            throw new DataAccessException("Error inesperado al eliminar campanias", ex);
        }
    }
}

