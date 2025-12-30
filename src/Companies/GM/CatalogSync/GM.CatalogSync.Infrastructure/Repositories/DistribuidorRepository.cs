using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.CatalogSync.Infrastructure.Repositories;

/// <summary>
/// Repository para acceso a datos de Distribuidor (Dealer) usando Dapper.
/// </summary>
public class DistribuidorRepository : IDistribuidorRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<DistribuidorRepository> _logger;

    private const string TABLA = "CO_DISTRIBUIDORES";

    public DistribuidorRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<DistribuidorRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Distribuidor?> ObtenerPorDealerBacAsync(string dealerBac)
    {
        const string sql = @"
            SELECT 
                DEALERID as DealerId,
                DEALERID as DealerBac,
                CODI_NOMBRE as Nombre,
                CODI_RAZONSOCIAL as RazonSocial,
                CODI_RFC as Rfc,
                CODI_ZONA as Zona,
                CODI_SITECODE as SiteCode,
                CODI_DMS as Dms,
                CODI_NOMBRE as NombreDealer,
                CODI_MARCA as Marca
            FROM CO_DISTRIBUIDORES
            WHERE DEALERID = :DealerBac
            AND ROWNUM = 1";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo distribuidor por DealerBac: {DealerBac}", dealerBac);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<Distribuidor>(sql, new { DealerBac = dealerBac });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] Distribuidor {DealerBac} no encontrado", dealerBac);
                return null;
            }

            _logger.LogInformation("✅ [REPOSITORY] Distribuidor {DealerBac} obtenido exitosamente. Nombre: {Nombre}, DMS: {Dms}",
                resultado.DealerBac, resultado.NombreDealer ?? resultado.Nombre, resultado.Dms);

            return resultado;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle en {Method}. ErrorCode: {ErrorCode}",
                nameof(ObtenerPorDealerBacAsync), ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<List<Distribuidor>> ObtenerTodosAsync(int? empresaId = null, string? usuario = null)
    {
        var sql = @"
            SELECT 
                DEALERID as DealerId,
                DEALERID as DealerBac,
                CODI_NOMBRE as Nombre,
                CODI_RAZONSOCIAL as RazonSocial,
                CODI_RFC as Rfc,
                CODI_ZONA as Zona,
                CODI_SITECODE as SiteCode,
                CODI_DMS as Dms,
                CODI_NOMBRE as NombreDealer,
                CODI_MARCA as Marca
            FROM CO_DISTRIBUIDORES
            WHERE 1=1";

        var parameters = new DynamicParameters();

        // Filtro por empresa si se proporciona
        if (empresaId.HasValue)
        {
            sql += " AND EMPR_EMPRESAID = :EmpresaId";
            parameters.Add("EmpresaId", empresaId.Value);
        }

        // Filtro por usuario si se proporciona (solo distribuidores asociados al usuario)
        if (!string.IsNullOrWhiteSpace(usuario))
        {
            sql += @" AND DEALERID IN (
                SELECT COUD_DEALER 
                FROM CO_USUARIOXDEALER 
                WHERE COUD_USUARIO = :Usuario";
            
            if (empresaId.HasValue)
            {
                sql += " AND EMPR_EMPRESAID = :EmpresaId";
            }
            
            sql += ")";
            parameters.Add("Usuario", usuario.Trim());
        }

        sql += " ORDER BY DEALERID, CODI_NOMBRE";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Obteniendo todos los distribuidores. EmpresaId: {EmpresaId}, Usuario: {Usuario}",
                empresaId?.ToString() ?? "Todos", usuario ?? "Todos");

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultados = await connection.QueryAsync<Distribuidor>(sql, parameters);
            var lista = resultados.ToList();

            _logger.LogInformation(
                "✅ [REPOSITORY] Se obtuvieron {Cantidad} distribuidores",
                lista.Count);

            return lista;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle en {Method}. ErrorCode: {ErrorCode}",
                nameof(ObtenerTodosAsync), ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }
}

