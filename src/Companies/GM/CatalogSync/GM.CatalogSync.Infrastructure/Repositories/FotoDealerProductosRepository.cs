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
                f.COFD_FOTODEALERPRODUCTOSID as FotoDealerProductosId,
                f.COFD_COCA_CARGAARCHIVOSINID as CargaArchivoSincronizacionId,
                f.COSA_DEALERBAC as DealerBac,
                f.COFD_NOMBREDEALER as NombreDealer,
                f.COFD_RAZONSOCIALDEALER as RazonSocialDealer,
                f.COFD_DMS as Dms,
                f.COFD_FECHAREGISTRO as FechaRegistro,
                f.FECHAALTA as FechaAlta,
                f.USUARIOALTA as UsuarioAlta,
                f.FECHAMODIFICACION as FechaModificacion,
                f.USUARIOMODIFICACION as UsuarioModificacion,
                c.COCA_IDCARGA as IdCarga,
                c.COCA_PROCESO as ProcesoCarga,
                c.COCA_FECHACARGA as FechaCarga,
                s.COSA_FECHASINCRONIZACION as FechaSincronizacion,
                CASE 
                    WHEN s.COSA_FECHASINCRONIZACION IS NOT NULL AND c.COCA_FECHACARGA IS NOT NULL 
                    THEN ROUND((s.COSA_FECHASINCRONIZACION - c.COCA_FECHACARGA) * 24, 2)
                    ELSE NULL
                END as TiempoSincronizacionHoras
            FROM CO_FOTODEALERPRODUCTOS f
            INNER JOIN CO_CARGAARCHIVOSINCRONIZACION c ON f.COFD_COCA_CARGAARCHIVOSINID = c.COCA_CARGAARCHIVOSINID
            LEFT JOIN CO_SINCRONIZACIONARCHIVOSDEALERS s ON f.COSA_DEALERBAC = s.COSA_DEALERBAC 
                AND f.COFD_COCA_CARGAARCHIVOSINID = s.COSA_COCA_CARGAARCHIVOSINID
            WHERE f.COFD_FOTODEALERPRODUCTOSID = :Id";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo foto dealer productos por ID: {Id}", id);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<FotoDealerProductosMap>(sql, new { Id = id });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] Foto dealer productos con ID {Id} no encontrado", id);
                return null;
            }

            // Mapear a entidad (sin los campos del JOIN)
            var entidad = new FotoDealerProductos
            {
                FotoDealerProductosId = resultado.FotoDealerProductosId,
                CargaArchivoSincronizacionId = resultado.CargaArchivoSincronizacionId,
                DealerBac = resultado.DealerBac,
                NombreDealer = resultado.NombreDealer,
                RazonSocialDealer = resultado.RazonSocialDealer,
                Dms = resultado.Dms,
                FechaRegistro = resultado.FechaRegistro,
                FechaAlta = resultado.FechaAlta,
                UsuarioAlta = resultado.UsuarioAlta,
                FechaModificacion = resultado.FechaModificacion,
                UsuarioModificacion = resultado.UsuarioModificacion
            };

            _logger.LogInformation("✅ [REPOSITORY] Foto dealer productos con ID {Id} obtenido exitosamente", id);
            return entidad;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al obtener registro por ID {Id}. ErrorCode: {ErrorCode}",
                id, ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<FotoDealerProductosMap?> ObtenerPorIdCompletoAsync(int id)
    {
        const string sql = @"
            SELECT 
                f.COFD_FOTODEALERPRODUCTOSID as FotoDealerProductosId,
                f.COFD_COCA_CARGAARCHIVOSINID as CargaArchivoSincronizacionId,
                f.COSA_DEALERBAC as DealerBac,
                f.COFD_NOMBREDEALER as NombreDealer,
                f.COFD_RAZONSOCIALDEALER as RazonSocialDealer,
                f.COFD_DMS as Dms,
                f.COFD_FECHAREGISTRO as FechaRegistro,
                f.FECHAALTA as FechaAlta,
                f.USUARIOALTA as UsuarioAlta,
                f.FECHAMODIFICACION as FechaModificacion,
                f.USUARIOMODIFICACION as UsuarioModificacion,
                c.COCA_IDCARGA as IdCarga,
                c.COCA_PROCESO as ProcesoCarga,
                c.COCA_FECHACARGA as FechaCarga,
                s.COSA_FECHASINCRONIZACION as FechaSincronizacion,
                CASE 
                    WHEN s.COSA_FECHASINCRONIZACION IS NOT NULL AND c.COCA_FECHACARGA IS NOT NULL 
                    THEN ROUND((s.COSA_FECHASINCRONIZACION - c.COCA_FECHACARGA) * 24, 2)
                    ELSE NULL
                END as TiempoSincronizacionHoras
            FROM CO_FOTODEALERPRODUCTOS f
            INNER JOIN CO_CARGAARCHIVOSINCRONIZACION c ON f.COFD_COCA_CARGAARCHIVOSINID = c.COCA_CARGAARCHIVOSINID
            LEFT JOIN CO_SINCRONIZACIONARCHIVOSDEALERS s ON f.COSA_DEALERBAC = s.COSA_DEALERBAC 
                AND f.COFD_COCA_CARGAARCHIVOSINID = s.COSA_COCA_CARGAARCHIVOSINID
            WHERE f.COFD_FOTODEALERPRODUCTOSID = :Id";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo foto dealer productos completo por ID: {Id}", id);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<FotoDealerProductosMap>(sql, new { Id = id });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] Foto dealer productos con ID {Id} no encontrado", id);
                return null;
            }

            _logger.LogInformation("✅ [REPOSITORY] Foto dealer productos completo con ID {Id} obtenido exitosamente", id);
            return resultado;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al obtener registro completo por ID {Id}. ErrorCode: {ErrorCode}",
                id, ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<(List<FotoDealerProductosMap> data, int totalRecords)> ObtenerTodosConFiltrosCompletoAsync(
        int? cargaArchivoSincronizacionId = null,
        string? dealerBac = null,
        string? dms = null,
        int page = 1,
        int pageSize = 200)
    {
        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Obteniendo fotos dealer productos completos con filtros. CargaArchivoSincId: {CargaId}, DealerBac: {DealerBac}, DMS: {Dms}, Página: {Page}, PageSize: {PageSize}",
                cargaArchivoSincronizacionId?.ToString() ?? "null",
                dealerBac ?? "null",
                dms ?? "null",
                page,
                pageSize);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (cargaArchivoSincronizacionId.HasValue)
            {
                whereClause += " AND f.COFD_COCA_CARGAARCHIVOSINID = :CargaArchivoSincronizacionId";
                parameters.Add("CargaArchivoSincronizacionId", cargaArchivoSincronizacionId.Value);
            }

            if (!string.IsNullOrWhiteSpace(dealerBac))
            {
                whereClause += " AND UPPER(f.COSA_DEALERBAC) LIKE UPPER(:DealerBac)";
                parameters.Add("DealerBac", $"%{dealerBac}%");
            }

            if (!string.IsNullOrWhiteSpace(dms))
            {
                whereClause += " AND UPPER(f.COFD_DMS) LIKE UPPER(:Dms)";
                parameters.Add("Dms", $"%{dms}%");
            }

            // Obtener total de registros
            var countSql = $"SELECT COUNT(*) FROM {TABLA} f {whereClause}";
            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            if (totalRecords == 0)
            {
                _logger.LogInformation("ℹ️ [REPOSITORY] No se encontraron registros con los filtros especificados");
                return (new List<FotoDealerProductosMap>(), 0);
            }

            // Aplicar paginación
            int offset = (page - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", offset + pageSize);

            var sql = $@"
                SELECT * FROM (
                    SELECT 
                        f.COFD_FOTODEALERPRODUCTOSID as FotoDealerProductosId,
                        f.COFD_COCA_CARGAARCHIVOSINID as CargaArchivoSincronizacionId,
                        f.COSA_DEALERBAC as DealerBac,
                        f.COFD_NOMBREDEALER as NombreDealer,
                        f.COFD_RAZONSOCIALDEALER as RazonSocialDealer,
                        f.COFD_DMS as Dms,
                        f.COFD_FECHAREGISTRO as FechaRegistro,
                        f.FECHAALTA as FechaAlta,
                        f.USUARIOALTA as UsuarioAlta,
                        f.FECHAMODIFICACION as FechaModificacion,
                        f.USUARIOMODIFICACION as UsuarioModificacion,
                        c.COCA_IDCARGA as IdCarga,
                        c.COCA_PROCESO as ProcesoCarga,
                        c.COCA_FECHACARGA as FechaCarga,
                        s.COSA_FECHASINCRONIZACION as FechaSincronizacion,
                        CASE 
                            WHEN s.COSA_FECHASINCRONIZACION IS NOT NULL AND c.COCA_FECHACARGA IS NOT NULL 
                            THEN ROUND((s.COSA_FECHASINCRONIZACION - c.COCA_FECHACARGA) * 24, 2)
                            ELSE NULL
                        END as TiempoSincronizacionHoras,
                        ROW_NUMBER() OVER (ORDER BY f.COFD_FOTODEALERPRODUCTOSID DESC) AS RNUM
                    FROM {TABLA} f
                    INNER JOIN CO_CARGAARCHIVOSINCRONIZACION c ON f.COFD_COCA_CARGAARCHIVOSINID = c.COCA_CARGAARCHIVOSINID
                    LEFT JOIN CO_SINCRONIZACIONARCHIVOSDEALERS s ON f.COSA_DEALERBAC = s.COSA_DEALERBAC 
                        AND f.COFD_COCA_CARGAARCHIVOSINID = s.COSA_COCA_CARGAARCHIVOSINID
                    {whereClause}
                ) WHERE RNUM > :offset AND RNUM <= :limit";

            var resultados = await connection.QueryAsync<FotoDealerProductosMap>(sql, parameters);
            var lista = resultados.ToList();

            _logger.LogInformation("✅ [REPOSITORY] Se obtuvieron {Cantidad} registros completos de {Total} totales (Página {Page})",
                lista.Count, totalRecords, page);

            return (lista, totalRecords);
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al obtener registros completos con filtros. ErrorCode: {ErrorCode}",
                ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExisteCargaArchivoSincronizacionIdAsync(int cargaArchivoSincronizacionId)
    {
        const string sql = @"
            SELECT COUNT(*) 
            FROM CO_CARGAARCHIVOSINCRONIZACION 
            WHERE COCA_CARGAARCHIVOSINID = :CargaArchivoSincronizacionId";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Verificando existencia de CargaArchivoSincronizacionId: {Id}", cargaArchivoSincronizacionId);

            using var connection = await _connectionFactory.CreateConnectionAsync();
            var count = await connection.ExecuteScalarAsync<int>(sql, new { CargaArchivoSincronizacionId = cargaArchivoSincronizacionId });

            var existe = count > 0;
            _logger.LogInformation("✅ [REPOSITORY] CargaArchivoSincronizacionId {Id} existe: {Existe}", cargaArchivoSincronizacionId, existe);
            return existe;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al verificar existencia de CargaArchivoSincronizacionId {Id}. ErrorCode: {ErrorCode}",
                cargaArchivoSincronizacionId, ex.Number);
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
                        f.COFD_FOTODEALERPRODUCTOSID as FotoDealerProductosId,
                        f.COFD_COCA_CARGAARCHIVOSINID as CargaArchivoSincronizacionId,
                        f.COSA_DEALERBAC as DealerBac,
                        f.COFD_NOMBREDEALER as NombreDealer,
                        f.COFD_RAZONSOCIALDEALER as RazonSocialDealer,
                        f.COFD_DMS as Dms,
                        f.COFD_FECHAREGISTRO as FechaRegistro,
                        f.FECHAALTA as FechaAlta,
                        f.USUARIOALTA as UsuarioAlta,
                        f.FECHAMODIFICACION as FechaModificacion,
                        f.USUARIOMODIFICACION as UsuarioModificacion,
                        c.COCA_IDCARGA as IdCarga,
                        c.COCA_PROCESO as ProcesoCarga,
                        c.COCA_FECHACARGA as FechaCarga,
                        s.COSA_FECHASINCRONIZACION as FechaSincronizacion,
                        CASE 
                            WHEN s.COSA_FECHASINCRONIZACION IS NOT NULL AND c.COCA_FECHACARGA IS NOT NULL 
                            THEN ROUND((s.COSA_FECHASINCRONIZACION - c.COCA_FECHACARGA) * 24, 2)
                            ELSE NULL
                        END as TiempoSincronizacionHoras,
                        ROW_NUMBER() OVER (ORDER BY f.COFD_FOTODEALERPRODUCTOSID DESC) AS RNUM
                    FROM {TABLA} f
                    INNER JOIN CO_CARGAARCHIVOSINCRONIZACION c ON f.COFD_COCA_CARGAARCHIVOSINID = c.COCA_CARGAARCHIVOSINID
                    LEFT JOIN CO_SINCRONIZACIONARCHIVOSDEALERS s ON f.COSA_DEALERBAC = s.COSA_DEALERBAC 
                        AND f.COFD_COCA_CARGAARCHIVOSINID = s.COSA_COCA_CARGAARCHIVOSINID
                    {whereClause}
                ) WHERE RNUM > :offset AND RNUM <= :limit";

            var resultados = await connection.QueryAsync<FotoDealerProductosMap>(sql, parameters);
            var lista = resultados.Select(r => new FotoDealerProductos
            {
                FotoDealerProductosId = r.FotoDealerProductosId,
                CargaArchivoSincronizacionId = r.CargaArchivoSincronizacionId,
                DealerBac = r.DealerBac,
                NombreDealer = r.NombreDealer,
                RazonSocialDealer = r.RazonSocialDealer,
                Dms = r.Dms,
                FechaRegistro = r.FechaRegistro,
                FechaAlta = r.FechaAlta,
                UsuarioAlta = r.UsuarioAlta,
                FechaModificacion = r.FechaModificacion,
                UsuarioModificacion = r.UsuarioModificacion
            }).ToList();

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

