using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;
using Microsoft.Extensions.Logging;

namespace GM.CatalogSync.Infrastructure.Repositories;

/// <summary>
/// Repository para acceso a datos de Producto usando Dapper
/// </summary>
public class ProductoRepository : IProductoRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<ProductoRepository> _logger;

    public ProductoRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<ProductoRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<(List<Producto> productos, int totalRecords)> GetByFiltersAsync(
        string? pais,
        string? marcaNegocio,
        int? anioModelo,
        int page,
        int pageSize,
        string correlationId)
    {
        try
        {
            _logger.LogInformation("[{CorrelationId}] 🗄️ [REPOSITORY] Consultando productos - País: {Pais}, Marca: {Marca}, Año: {Anio}, Página: {Page}",
                correlationId, pais ?? "Todos", marcaNegocio ?? "Todas", anioModelo?.ToString() ?? "Todos", page);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(pais))
            {
                whereClause += " AND COGM_PAIS = :pais";
                parameters.Add("pais", pais);
            }

            if (!string.IsNullOrWhiteSpace(marcaNegocio))
            {
                whereClause += " AND COGM_MARCANEGOCIO = :marcaNegocio";
                parameters.Add("marcaNegocio", marcaNegocio);
            }

            if (anioModelo.HasValue)
            {
                whereClause += " AND COGM_ANIOMODELO = :anioModelo";
                parameters.Add("anioModelo", anioModelo.Value);
            }

            // Obtener total de registros
            var countSql = $"SELECT COUNT(*) FROM CO_GM_LISTAPRODUCTOS {whereClause}";
            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            if (totalRecords == 0)
            {
                return (new List<Producto>(), 0);
            }

            int offset = (page - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", offset + pageSize);

            var sql = $@"
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
                        FECHAALTA as FechaAlta,
                        USUARIOALTA as UsuarioAlta,
                        FECHAMODIFICACION as FechaModificacion,
                        USUARIOMODIFICACION as UsuarioModificacion,
                        ROW_NUMBER() OVER (ORDER BY COGM_PRODUCTOID) AS RNUM
                    FROM CO_GM_LISTAPRODUCTOS
                    {whereClause}
                ) WHERE RNUM > :offset AND RNUM <= :limit";

            var productos = await connection.QueryAsync<Producto>(sql, parameters);

            _logger.LogInformation("[{CorrelationId}] ✅ [REPOSITORY] Consulta completada - {Count} registros de {Total} totales",
                correlationId, productos.Count(), totalRecords);

            return (productos.ToList(), totalRecords);
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error Oracle en GetByFiltersAsync", correlationId);
            throw new DataAccessException("Error al consultar productos", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error inesperado en GetByFiltersAsync", correlationId);
            throw new DataAccessException("Error inesperado al consultar productos", ex);
        }
    }

    public async Task<int> GetTotalCountAsync(
        string? pais,
        string? marcaNegocio,
        int? anioModelo,
        string correlationId)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(pais))
            {
                whereClause += " AND COGM_PAIS = :pais";
                parameters.Add("pais", pais);
            }

            if (!string.IsNullOrWhiteSpace(marcaNegocio))
            {
                whereClause += " AND COGM_MARCANEGOCIO = :marcaNegocio";
                parameters.Add("marcaNegocio", marcaNegocio);
            }

            if (anioModelo.HasValue)
            {
                whereClause += " AND COGM_ANIOMODELO = :anioModelo";
                parameters.Add("anioModelo", anioModelo.Value);
            }

            var sql = $"SELECT COUNT(*) FROM CO_GM_LISTAPRODUCTOS {whereClause}";
            var count = await connection.ExecuteScalarAsync<int>(sql, parameters);

            return count;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error Oracle en GetTotalCountAsync", correlationId);
            throw new DataAccessException("Error al contar productos", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error inesperado en GetTotalCountAsync", correlationId);
            throw new DataAccessException("Error inesperado al contar productos", ex);
        }
    }

    public async Task<bool> ExistsByProductoAnioAndLocalAsync(
        string nombreProducto,
        int anioModelo,
        string? nombreLocal,
        string correlationId)
    {
        try
        {
            var nombreProductoNormalizado = (nombreProducto ?? string.Empty).Trim();
            var nombreLocalNormalizado = string.IsNullOrWhiteSpace(nombreLocal) ? null : nombreLocal.Trim();

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                SELECT COUNT(*) 
                FROM CO_GM_LISTAPRODUCTOS 
                WHERE COGM_NOMBREPRODUCTO = :nombreProducto
                  AND COGM_ANIOMODELO = :anioModelo
                  AND (COGM_NOMBRELOCAL = :nombreLocal OR (COGM_NOMBRELOCAL IS NULL AND :nombreLocal IS NULL))";

            var parameters = new DynamicParameters();
            parameters.Add("nombreProducto", nombreProductoNormalizado);
            parameters.Add("anioModelo", anioModelo);
            parameters.Add("nombreLocal", nombreLocalNormalizado ?? (object)DBNull.Value);

            var count = await connection.ExecuteScalarAsync<int>(sql, parameters);
            return count > 0;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error Oracle en ExistsByProductoAnioAndLocalAsync", correlationId);
            throw new DataAccessException("Error al verificar existencia", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error inesperado en ExistsByProductoAnioAndLocalAsync", correlationId);
            throw new DataAccessException("Error inesperado al verificar existencia", ex);
        }
    }

    public async Task<int> InsertAsync(
        Producto producto,
        string currentUser,
        string correlationId)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = @"
                INSERT INTO CO_GM_LISTAPRODUCTOS (
                    COGM_NOMBREPRODUCTO, COGM_PAIS, COGM_NOMBREMODELO, COGM_ANIOMODELO,
                    COGM_MODELOINTERES, COGM_MARCANEGOCIO, COGM_NOMBRELOCAL, COGM_DEFINICIONVEHICULO,
                    FECHAALTA, USUARIOALTA
                ) VALUES (
                    :nombreProducto, :pais, :nombreModelo, :anioModelo,
                    :modeloInteres, :marcaNegocio, :nombreLocal, :definicionVehiculo,
                    SYSDATE, :usuarioAlta
                )";

            var parameters = new DynamicParameters();
            parameters.Add("nombreProducto", producto.NombreProducto);
            parameters.Add("pais", producto.Pais);
            parameters.Add("nombreModelo", producto.NombreModelo);
            parameters.Add("anioModelo", producto.AnioModelo);
            parameters.Add("modeloInteres", producto.ModeloInteres);
            parameters.Add("marcaNegocio", producto.MarcaNegocio);
            parameters.Add("nombreLocal", producto.NombreLocal ?? (object)DBNull.Value);
            parameters.Add("definicionVehiculo", producto.DefinicionVehiculo ?? (object)DBNull.Value);
            parameters.Add("usuarioAlta", currentUser ?? "SYSTEM");

            var rowsAffected = await connection.ExecuteAsync(sql, parameters);
            return rowsAffected;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error Oracle en InsertAsync", correlationId);
            throw new DataAccessException("Error al insertar producto", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error inesperado en InsertAsync", correlationId);
            throw new DataAccessException("Error inesperado al insertar producto", ex);
        }
    }

    public async Task<int> UpsertBatchWithTransactionAsync(
        IEnumerable<Producto> productos,
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
                    INSERT INTO CO_GM_LISTAPRODUCTOS (
                        COGM_NOMBREPRODUCTO, COGM_PAIS, COGM_NOMBREMODELO, COGM_ANIOMODELO,
                        COGM_MODELOINTERES, COGM_MARCANEGOCIO, COGM_NOMBRELOCAL, COGM_DEFINICIONVEHICULO,
                        FECHAALTA, USUARIOALTA
                    ) VALUES (
                        :nombreProducto, :pais, :nombreModelo, :anioModelo,
                        :modeloInteres, :marcaNegocio, :nombreLocal, :definicionVehiculo,
                        SYSDATE, :usuarioAlta
                    )";

                int totalInserted = 0;
                foreach (var producto in productos)
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("nombreProducto", producto.NombreProducto);
                    parameters.Add("pais", producto.Pais);
                    parameters.Add("nombreModelo", producto.NombreModelo);
                    parameters.Add("anioModelo", producto.AnioModelo);
                    parameters.Add("modeloInteres", producto.ModeloInteres);
                    parameters.Add("marcaNegocio", producto.MarcaNegocio);
                    parameters.Add("nombreLocal", producto.NombreLocal ?? (object)DBNull.Value);
                    parameters.Add("definicionVehiculo", producto.DefinicionVehiculo ?? (object)DBNull.Value);
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
            throw new DataAccessException("Error al insertar productos en lote", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error inesperado en UpsertBatchWithTransactionAsync", correlationId);
            throw new DataAccessException("Error inesperado al insertar productos en lote", ex);
        }
    }

    public async Task<int> DeleteAllAsync(
        string currentUser,
        string correlationId)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var sql = "DELETE FROM CO_GM_LISTAPRODUCTOS";
            var rowsAffected = await connection.ExecuteAsync(sql);

            _logger.LogInformation("[{CorrelationId}] ✅ [REPOSITORY] DELETE completado - {Rows} filas eliminadas",
                correlationId, rowsAffected);

            return rowsAffected;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error Oracle en DeleteAllAsync", correlationId);
            throw new DataAccessException("Error al eliminar productos", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error inesperado en DeleteAllAsync", correlationId);
            throw new DataAccessException("Error inesperado al eliminar productos", ex);
        }
    }
}

