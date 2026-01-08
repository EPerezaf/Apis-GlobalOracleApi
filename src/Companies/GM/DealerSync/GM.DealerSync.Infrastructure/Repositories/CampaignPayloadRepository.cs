using Dapper;
using GM.DealerSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Infrastructure;

namespace GM.DealerSync.Infrastructure.Repositories;

/// <summary>
/// Repository para obtener campañas desde CO_CAMPAIGNCATALOG para generar payload
/// </summary>
public class CampaignPayloadRepository : ICampaignPayloadRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<CampaignPayloadRepository> _logger;

    public CampaignPayloadRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<CampaignPayloadRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<List<CampaignPayload>> GetAllCampaignsAsync()
    {
        const string sql = @"
            SELECT 
                COCC_SOURCECODEID as SourceCodeId,
                COCC_ID as Id,
                COCC_NAME as Name,
                COCC_RECORDTYPEID as RecordTypeId,
                COCC_LEADRECORDTYPE as LeadRecordType,
                COCC_LEADENQUIRYTYPE as LeadEnquiryType,
                COCC_LEADSOURCE as LeadSource,
                COCC_LEADSOURCEDETAILS as LeadSourceDetails,
                COCC_STATUS as Status
            FROM LABGDMS.CO_CAMPAIGNCATALOG
            ORDER BY COCC_CAMPANIAID";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo todas las campañas para payload...");

            using var connection = await _connectionFactory.CreateConnectionAsync();
            var campaigns = await connection.QueryAsync<CampaignPayload>(sql);
            var lista = campaigns.ToList();

            _logger.LogInformation("✅ [REPOSITORY] {Cantidad} campañas obtenidas para payload", lista.Count);
            return lista;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al obtener campañas. ErrorCode: {ErrorCode}", ex.Number);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error inesperado al obtener campañas");
            throw;
        }
    }
}

