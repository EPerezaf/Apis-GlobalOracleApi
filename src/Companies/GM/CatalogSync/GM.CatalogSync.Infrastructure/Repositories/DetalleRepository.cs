using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.CatalogSync.Infrastructure.Repositories;

public class DetalleRepository : IDetalleRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<DetalleRepository> _logger;
    public DetalleRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<DetalleRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }
    

    public async Task<(List<DetalleDealer> dealers, int totalRecords)> GetByFilterAsync(
        string? dealerId,
        string? nombre,
        string? razonSocial,
        string? rfc,
        int? empresaId,
        int page,
        int pageSize,
        string currentUser,
        string correlationId)
    {
        try
        {
            _logger.LogInformation(
                "[{CorrelationId}] [REPOSITORY] Consultando dealers - Dealer Id: {DealerId}, Nombre: {Nombre}, Razon Social: {RazonSocial}, RFC: {Rfc}, Pagina: {Page}",
                dealerId ?? "Todos", nombre ?? "Todos", razonSocial ?? "Todos", rfc ?? "Todos", page, page);

                using var connection = await _connectionFactory.CreateConnectionAsync();

                var parameters = new DynamicParameters();
                var whereClause = "WHERE 1=1";

            if (empresaId.HasValue)
            {
                whereClause += " AND EMPR_EMPRESAID = :empresaId";
                parameters.Add("empresaId", empresaId);
                _logger.LogDebug("[{CorrelationId}] ✅ Aplicando filtro por empresa: {EmpresaId}", 
                    correlationId, empresaId.Value);
            }

            if (!string.IsNullOrWhiteSpace(dealerId))
            {
                whereClause += " AND DEALERID = :dealerId";
                parameters.Add("dealerId", dealerId);
            }

            if (!string.IsNullOrWhiteSpace(nombre))
            {
                whereClause += " AND CODI_NOMBRE = :nombre";
                parameters.Add("nombre", nombre);
            }

            if (!string.IsNullOrWhiteSpace(razonSocial))
            {
                whereClause += " AND CODI_RAZONSOCIAL = :razonSocial";
                parameters.Add("razonSocial", razonSocial);
            }

            if (!string.IsNullOrWhiteSpace(rfc))
            {
                whereClause += " AND CODI_RFC = :rfc";
                parameters.Add("rfc", rfc);
            }
            

            //Obtener total de registros 
            var countSql = $"SELECT COUNT(*) FROM LABGDMS.CO_DISTRIBUIDORES {whereClause}";
            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            if(totalRecords == 0)
            {
                return (new List<DetalleDealer>(), 0);
            }

            int offset = (page - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", offset + pageSize);

            var sql = $@"
                SELECT * FROM (
                    SELECT 
                        DEALERID as DealerId,
                        CODI_NOMBRE as Nombre,
                        CODI_RAZONSOCIAL as RazonSocial,
                        CODI_ZONA as Zona,
                        CODI_RFC as Rfc,
                        CODI_MARCA as Marca,
                        CODI_NODEALER as NoDealer,
                        CODI_SITECODE as SiteCode,
                        CODI_TIPO as Tipo,
                        CODI_MARCAS as Marcas,
                        CODI_DISTRITO as Distrito,
                        EMPR_EMPRESAID as EmpresaId,
                        CODI_DMS as Dms,
                        CODI_CLIENTID as ClienteId,
                        CODI_CLIENTSECRET as CleinteSecreto,
                        ROW_NUMBER() OVER (ORDER BY DEALERID) AS RNUM
                    FROM LABGDMS.CO_DISTRIBUIDORES
                    {whereClause}
                ) WHERE RNUM > :offset AND RNUM <= :limit";

            var dealers = await connection.QueryAsync<DetalleDealer>(sql,parameters);

            _logger.LogInformation("[{CorrelationId}] [REPOSITORY] Conuslta completada - {Count} registros de {Total} totales",
                correlationId, dealers.Count(), totalRecords);

            return (dealers.ToList(), totalRecords);
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error Oracle  en GetByFilterAsync", correlationId);
            throw new DataAccessException("Error al consultar dealers", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,"[{CorrelationId}] [REPOSITORY] Error inesperado en GetByFilterAsync", correlationId);
            throw new DataAccessException("Error inesperado al consultar dealers",ex);
        }
    }
    
}