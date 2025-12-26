using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.DealersSincronizacion.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.DealersSincronizacion.Infrastructure.Repositories;

/// <summary>
/// Repository para acceso a datos de Producto usando Dapper (para dealers).
/// </summary>
public class ProductoRepository : IProductoRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<ProductoRepository> _logger;

    private const string TABLA = "CO_GM_LISTAPRODUCTOS";

    public ProductoRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<ProductoRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(List<Producto> productos, int totalRecords)> ObtenerTodosAsync(
        int page = 1,
        int pageSize = 200)
    {
        const string countSql = @"
            SELECT COUNT(*) 
            FROM CO_GM_LISTAPRODUCTOS";

        const string sql = @"
            SELECT * FROM (
                SELECT 
                    COGM_PRODUCTOID as ProductoId,
                    COGM_NOMBREPRODUCTO as NombreProducto,
                    COGM_PAIS as Pais,
                    COGM_NOMBREMODELO as NombreModelo,
                    COGM_ANIOMODELO as AnioModelo,
                    COGM_MODELOINTERES as ModeloInteres,
                    COGM_MARCANEGOCIO as MarcaNegocio,
                    COGM_NOMBRELOCAL as NombreLocal,
                    COGM_DEFINICIONVEHICULO as DefinicionVehiculo,
                    ROW_NUMBER() OVER (ORDER BY COGM_PRODUCTOID DESC) AS RNUM
                FROM CO_GM_LISTAPRODUCTOS
            ) WHERE RNUM > :offset AND RNUM <= :limit";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo productos activos. Página: {Page}, PageSize: {PageSize}", page, pageSize);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql);

            if (totalRecords == 0)
            {
                _logger.LogInformation("⚠️ [REPOSITORY] No se encontraron productos activos");
                return (new List<Producto>(), 0);
            }

            int offset = (page - 1) * pageSize;
            var parameters = new { offset, limit = offset + pageSize };

            var resultados = await connection.QueryAsync<Producto>(sql, parameters);
            var lista = resultados.ToList();

            _logger.LogInformation("✅ [REPOSITORY] {Cantidad} productos obtenidos de {Total} totales", lista.Count, totalRecords);
            return (lista, totalRecords);
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle en {Method}. ErrorCode: {ErrorCode}",
                nameof(ObtenerTodosAsync), ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }
}

