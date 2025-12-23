using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;
using ValidationError = Shared.Exceptions.ValidationError;

namespace GM.CatalogSync.Infrastructure.Repositories;

/// <summary>
/// Repository para acceso a datos de Foto de Dealer Productos usando Dapper.
/// Tabla: CO_FOTODEALERPRODUCTOS
/// </summary>
public class FotoDealerProductosRepository : IFotoDealerProductosRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<FotoDealerProductosRepository> _logger;

    private const string TABLA = "CO_FOTODEALERPRODUCTOS";
    private const string SECUENCIA = "SEQ_COFD_FOTODEALERPRODUCTOSID";

    public FotoDealerProductosRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<FotoDealerProductosRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FotoDealerProductos?> ObtenerPorIdAsync(int id)
    {
        const string sql = @"
            SELECT 
                COFD_FOTODEALERPRODUCTOSID as FotoDealerProductosId,
                COFD_COCA_CARGAARCHIVOSINID as CargaArchivoSincronizacionId,
                COSA_DEALERBAC as DealerBac,
                COFD_NOMBREDEALER as NombreDealer,
                COFD_RAZONSOCIALDEALER as RazonSocialDealer,
                COFD_DMS as Dms,
                COFD_FECHAREGISTRO as FechaRegistro,
                FECHAALTA as FechaAlta,
                USUARIOALTA as UsuarioAlta,
                FECHAMODIFICACION as FechaModificacion,
                USUARIOMODIFICACION as UsuarioModificacion
            FROM CO_FOTODEALERPRODUCTOS
            WHERE COFD_FOTODEALERPRODUCTOSID = :Id";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo foto dealer productos por ID: {Id}", id);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<FotoDealerProductos>(sql, new { Id = id });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] Foto dealer productos con ID {Id} no encontrado", id);
                return null;
            }

            _logger.LogInformation("✅ [REPOSITORY] Foto dealer productos con ID {Id} obtenido exitosamente", id);
            return resultado;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al obtener registro por ID {Id}. ErrorCode: {ErrorCode}",
                id, ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<(List<FotoDealerProductos> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        int? cargaArchivoSincronizacionId = null,
        string? dealerBac = null,
        string? dms = null,
        int page = 1,
        int pageSize = 200)
    {
        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Consultando fotos dealer productos - CargaArchivoSincId: {CargaId}, DealerBac: {DealerBac}, DMS: {Dms}, Página: {Page}, PageSize: {PageSize}",
                cargaArchivoSincronizacionId?.ToString() ?? "Todos",
                dealerBac ?? "Todos",
                dms ?? "Todos",
                page,
                pageSize);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (cargaArchivoSincronizacionId.HasValue)
            {
                whereClause += " AND COFD_COCA_CARGAARCHIVOSINID = :CargaArchivoSincronizacionId";
                parameters.Add("CargaArchivoSincronizacionId", cargaArchivoSincronizacionId.Value);
            }

            if (!string.IsNullOrWhiteSpace(dealerBac))
            {
                whereClause += " AND UPPER(COSA_DEALERBAC) LIKE UPPER(:DealerBac)";
                parameters.Add("DealerBac", $"%{dealerBac}%");
            }

            if (!string.IsNullOrWhiteSpace(dms))
            {
                whereClause += " AND UPPER(COFD_DMS) LIKE UPPER(:Dms)";
                parameters.Add("Dms", $"%{dms}%");
            }

            // Obtener total de registros
            var countSql = $"SELECT COUNT(*) FROM {TABLA} {whereClause}";
            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            if (totalRecords == 0)
            {
                _logger.LogInformation("ℹ️ [REPOSITORY] No se encontraron registros con los filtros especificados");
                return (new List<FotoDealerProductos>(), 0);
            }

            // Aplicar paginación
            int offset = (page - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", offset + pageSize);

            var sql = $@"
                SELECT * FROM (
                    SELECT 
                        COFD_FOTODEALERPRODUCTOSID as FotoDealerProductosId,
                        COFD_COCA_CARGAARCHIVOSINID as CargaArchivoSincronizacionId,
                        COSA_DEALERBAC as DealerBac,
                        COFD_NOMBREDEALER as NombreDealer,
                        COFD_RAZONSOCIALDEALER as RazonSocialDealer,
                        COFD_DMS as Dms,
                        COFD_FECHAREGISTRO as FechaRegistro,
                        FECHAALTA as FechaAlta,
                        USUARIOALTA as UsuarioAlta,
                        FECHAMODIFICACION as FechaModificacion,
                        USUARIOMODIFICACION as UsuarioModificacion,
                        ROW_NUMBER() OVER (ORDER BY COFD_FOTODEALERPRODUCTOSID DESC) AS RNUM
                    FROM {TABLA}
                    {whereClause}
                ) WHERE RNUM > :offset AND RNUM <= :limit";

            var resultados = await connection.QueryAsync<FotoDealerProductos>(sql, parameters);
            var lista = resultados.ToList();

            _logger.LogInformation(
                "✅ [REPOSITORY] {Cantidad} registros obtenidos de {Total} totales (Página {Page})",
                lista.Count, totalRecords, page);

            return (lista, totalRecords);
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex,
                "❌ [REPOSITORY] Error Oracle al consultar registros. ErrorCode: {ErrorCode}",
                ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExisteCombinacionAsync(int cargaArchivoSincronizacionId, string dealerBac)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM CO_FOTODEALERPRODUCTOS
            WHERE COFD_COCA_CARGAARCHIVOSINID = :CargaArchivoSincronizacionId
            AND COSA_DEALERBAC = :DealerBac";

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                CargaArchivoSincronizacionId = cargaArchivoSincronizacionId,
                DealerBac = dealerBac
            });

            return count > 0;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex,
                "❌ [REPOSITORY] Error Oracle al verificar combinación. CargaArchivoSincId: {CargaId}, DealerBac: {DealerBac}, ErrorCode: {ErrorCode}",
                cargaArchivoSincronizacionId, dealerBac, ex.Number);
            throw new DataAccessException("Error al verificar la existencia del registro en la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<List<FotoDealerProductos>> CrearBatchAsync(
        List<FotoDealerProductos> entidades,
        string usuarioAlta)
    {
        if (entidades == null || !entidades.Any())
        {
            throw new BusinessValidationException("La lista de entidades no puede estar vacía", new List<ValidationError>());
        }

        const string sqlInsert = @"
            INSERT INTO CO_FOTODEALERPRODUCTOS (
                COFD_FOTODEALERPRODUCTOSID,
                COFD_COCA_CARGAARCHIVOSINID,
                COSA_DEALERBAC,
                COFD_NOMBREDEALER,
                COFD_RAZONSOCIALDEALER,
                COFD_DMS,
                COFD_FECHAREGISTRO,
                FECHAALTA,
                USUARIOALTA
            ) VALUES (
                SEQ_COFD_FOTODEALERPRODUCTOSID.NEXTVAL,
                :CargaArchivoSincronizacionId,
                :DealerBac,
                :NombreDealer,
                :RazonSocialDealer,
                :Dms,
                :FechaRegistro,
                SYSDATE,
                :UsuarioAlta
            ) RETURNING COFD_FOTODEALERPRODUCTOSID INTO :Id";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Iniciando inserción batch de {Cantidad} registros. Usuario: {Usuario}",
                entidades.Count, usuarioAlta);

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var entidadesCreadas = new List<FotoDealerProductos>();

                foreach (var entidad in entidades)
                {
                    var parametros = new DynamicParameters();
                    parametros.Add("CargaArchivoSincronizacionId", entidad.CargaArchivoSincronizacionId);
                    parametros.Add("DealerBac", entidad.DealerBac);
                    parametros.Add("NombreDealer", entidad.NombreDealer);
                    parametros.Add("RazonSocialDealer", entidad.RazonSocialDealer);
                    parametros.Add("Dms", entidad.Dms);
                    parametros.Add("FechaRegistro", entidad.FechaRegistro);
                    parametros.Add("UsuarioAlta", usuarioAlta);
                    parametros.Add("Id", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

                    await connection.ExecuteAsync(sqlInsert, parametros, transaction);

                    var nuevoId = parametros.Get<int>("Id");
                    entidad.FotoDealerProductosId = nuevoId;
                    entidad.UsuarioAlta = usuarioAlta;
                    entidadesCreadas.Add(entidad);
                }

                // Commit de la transacción
                transaction.Commit();

                _logger.LogInformation(
                    "✅ [REPOSITORY] Batch insert completado exitosamente. {Cantidad} registros creados",
                    entidadesCreadas.Count);

                return entidadesCreadas;
            }
            catch (Exception ex)
            {
                // Rollback en caso de error
                transaction.Rollback();
                _logger.LogError(ex, "❌ [REPOSITORY] Rollback ejecutado debido a error en el batch insert");
                throw;
            }
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex,
                "❌ [REPOSITORY] Error Oracle en batch insert. Cantidad: {Cantidad}, ErrorCode: {ErrorCode}",
                entidades.Count, ex.Number);
            throw new DataAccessException("Error al crear los registros en la base de datos", ex);
        }
    }
}

