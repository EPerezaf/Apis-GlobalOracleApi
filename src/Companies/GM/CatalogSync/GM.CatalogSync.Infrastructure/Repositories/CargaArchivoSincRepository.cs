using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.CatalogSync.Infrastructure.Repositories;

/// <summary>
/// Repository para acceso a datos de Carga de Archivo de Sincronización usando Dapper.
/// Tabla: CO_CARGAARCHIVOSINCRONIZACION
/// </summary>
public class CargaArchivoSincRepository : ICargaArchivoSincRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<CargaArchivoSincRepository> _logger;

    private const string TABLA = "CO_CARGAARCHIVOSINCRONIZACION";
    private const string SECUENCIA = "SEQ_COCA_CARGAARCHIVOSINID";

    public CargaArchivoSincRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<CargaArchivoSincRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CargaArchivoSincronizacion?> ObtenerPorIdAsync(int id)
    {
        const string sql = @"
            SELECT 
                COCA_CARGAARCHIVOSINID as CargaArchivoSincronizacionId,
                COCA_PROCESO as Proceso,
                COCA_NOMBREARCHIVO as NombreArchivo,
                COCA_FECHACARGA as FechaCarga,
                COCA_IDCARGA as IdCarga,
                COCA_REGISTROS as Registros,
                COCA_ACTUAL as Actual,
                COCA_DEALERSTOTALES as DealersTotales,
                COCA_DEALERSSONCRONIZADOS as DealersSincronizados,
                COCA_PORCDEALERSSINC as PorcDealersSinc,
                FECHAALTA as FechaAlta,
                USUARIOALTA as UsuarioAlta,
                FECHAMODIFICACION as FechaModificacion,
                USUARIOMODIFICACION as UsuarioModificacion
            FROM CO_CARGAARCHIVOSINCRONIZACION
            WHERE COCA_CARGAARCHIVOSINID = :Id";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo registro de carga por ID: {Id}", id);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<CargaArchivoSincMap>(sql, new { Id = id });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] Registro de carga con ID {Id} no encontrado", id);
                return null;
            }

            _logger.LogInformation("✅ [REPOSITORY] Registro de carga con ID {Id} obtenido exitosamente", id);
            return MapearAEntidad(resultado);
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al obtener registro por ID {Id}. ErrorCode: {ErrorCode}",
                id, ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<(List<CargaArchivoSincronizacion> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        string? idCarga = null,
        bool? actual = null,
        int page = 1,
        int pageSize = 200)
    {
        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Consultando registros de carga - Proceso: {Proceso}, IdCarga: {IdCarga}, Actual: {Actual}, Página: {Page}, PageSize: {PageSize}",
                proceso ?? "Todos", idCarga ?? "Todos", actual?.ToString() ?? "Todos", page, pageSize);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(proceso))
            {
                whereClause += " AND UPPER(COCA_PROCESO) LIKE UPPER(:Proceso)";
                parameters.Add("Proceso", $"%{proceso}%");
            }

            if (!string.IsNullOrWhiteSpace(idCarga))
            {
                whereClause += " AND UPPER(COCA_IDCARGA) LIKE UPPER(:IdCarga)";
                parameters.Add("IdCarga", $"%{idCarga}%");
            }

            if (actual.HasValue)
            {
                whereClause += " AND COCA_ACTUAL = :Actual";
                parameters.Add("Actual", actual.Value ? 1 : 0);
            }

            // Obtener total de registros
            var countSql = $"SELECT COUNT(*) FROM {TABLA} {whereClause}";
            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            if (totalRecords == 0)
            {
                _logger.LogInformation("✅ [REPOSITORY] No se encontraron registros de carga");
                return (new List<CargaArchivoSincronizacion>(), 0);
            }

            // Aplicar paginación
            int offset = (page - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", offset + pageSize);

            var sql = $@"
                SELECT * FROM (
                    SELECT 
                        COCA_CARGAARCHIVOSINID as CargaArchivoSincronizacionId,
                        COCA_PROCESO as Proceso,
                        COCA_NOMBREARCHIVO as NombreArchivo,
                        COCA_FECHACARGA as FechaCarga,
                        COCA_IDCARGA as IdCarga,
                        COCA_REGISTROS as Registros,
                        COCA_ACTUAL as Actual,
                        COCA_DEALERSTOTALES as DealersTotales,
                        COCA_DEALERSSONCRONIZADOS as DealersSincronizados,
                        COCA_PORCDEALERSSINC as PorcDealersSinc,
                        FECHAALTA as FechaAlta,
                        USUARIOALTA as UsuarioAlta,
                        FECHAMODIFICACION as FechaModificacion,
                        USUARIOMODIFICACION as UsuarioModificacion,
                        ROW_NUMBER() OVER (ORDER BY COCA_FECHACARGA DESC) AS RNUM
                    FROM {TABLA}
                    {whereClause}
                ) WHERE RNUM > :offset AND RNUM <= :limit";

            var resultados = await connection.QueryAsync<CargaArchivoSincMap>(sql, parameters);
            var lista = resultados.Select(MapearAEntidad).ToList();

            _logger.LogInformation("✅ [REPOSITORY] Se obtuvieron {Cantidad} registros de carga de {Total} totales (Página {Page})", 
                lista.Count, totalRecords, page);
            return (lista, totalRecords);
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al obtener registros con filtros. ErrorCode: {ErrorCode}",
                ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExisteIdCargaAsync(string idCarga)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM CO_CARGAARCHIVOSINCRONIZACION 
            WHERE COCA_IDCARGA = :IdCarga";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Verificando existencia de IdCarga: {IdCarga}", idCarga);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var count = await connection.ExecuteScalarAsync<int>(sql, new { IdCarga = idCarga });

            var existe = count > 0;
            _logger.LogInformation("✅ [REPOSITORY] IdCarga '{IdCarga}' existe: {Existe}", idCarga, existe);

            return existe;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al verificar IdCarga. ErrorCode: {ErrorCode}",
                ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<CargaArchivoSincronizacion> CrearConTransaccionAsync(
        CargaArchivoSincronizacion entidad,
        string usuarioAlta)
    {
        const string sqlUpdate = @"
            UPDATE CO_CARGAARCHIVOSINCRONIZACION 
            SET COCA_ACTUAL = 0,
                FECHAMODIFICACION = SYSDATE,
                USUARIOMODIFICACION = :UsuarioModificacion
            WHERE COCA_PROCESO = :Proceso 
            AND COCA_ACTUAL = 1";

        const string sqlInsert = @"
            INSERT INTO CO_CARGAARCHIVOSINCRONIZACION (
                COCA_CARGAARCHIVOSINID,
                COCA_PROCESO,
                COCA_NOMBREARCHIVO,
                COCA_FECHACARGA,
                COCA_IDCARGA,
                COCA_REGISTROS,
                COCA_ACTUAL,
                COCA_DEALERSTOTALES,
                COCA_DEALERSSONCRONIZADOS,
                COCA_PORCDEALERSSINC,
                FECHAALTA,
                USUARIOALTA
            ) VALUES (
                SEQ_COCA_CARGAARCHIVOSINID.NEXTVAL,
                :Proceso,
                :NombreArchivo,
                :FechaCarga,
                :IdCarga,
                :Registros,
                1,
                :DealersTotales,
                0,
                0.00,
                SYSDATE,
                :UsuarioAlta
            ) RETURNING COCA_CARGAARCHIVOSINID INTO :Id";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Iniciando transacción para crear registro de carga. Proceso: {Proceso}, IdCarga: {IdCarga}, Usuario: {Usuario}",
                entidad.Proceso, entidad.IdCarga, usuarioAlta);

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Actualizar registros anteriores del mismo proceso a COCA_ACTUAL = 0
                var registrosActualizados = await connection.ExecuteAsync(
                    sqlUpdate,
                    new
                    {
                        Proceso = entidad.Proceso,
                        UsuarioModificacion = usuarioAlta
                    },
                    transaction);

                _logger.LogInformation(
                    "📝 [REPOSITORY] {Cantidad} registros anteriores actualizados a COCA_ACTUAL=0 para proceso: {Proceso}",
                    registrosActualizados, entidad.Proceso);

                // 2. Insertar nuevo registro con COCA_ACTUAL = 1
                var parametrosInsert = new DynamicParameters();
                parametrosInsert.Add("Proceso", entidad.Proceso);
                parametrosInsert.Add("NombreArchivo", entidad.NombreArchivo);
                parametrosInsert.Add("FechaCarga", entidad.FechaCarga);
                parametrosInsert.Add("IdCarga", entidad.IdCarga);
                parametrosInsert.Add("Registros", entidad.Registros);
                parametrosInsert.Add("DealersTotales", entidad.DealersTotales);
                parametrosInsert.Add("UsuarioAlta", usuarioAlta);
                parametrosInsert.Add("Id", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

                await connection.ExecuteAsync(sqlInsert, parametrosInsert, transaction);

                var nuevoId = parametrosInsert.Get<int>("Id");

                // 3. Commit de la transacción
                transaction.Commit();

                _logger.LogInformation(
                    "✅ [REPOSITORY] Transacción completada exitosamente. Nuevo ID: {Id}, Proceso: {Proceso}",
                    nuevoId, entidad.Proceso);

                // Obtener el registro creado
                var registroCreado = await ObtenerPorIdAsync(nuevoId);

                if (registroCreado == null)
                {
                    throw new DataAccessException("No se pudo obtener el registro recién creado");
                }

                return registroCreado;
            }
            catch (Exception)
            {
                // Rollback en caso de error
                transaction.Rollback();
                _logger.LogError("❌ [REPOSITORY] Rollback ejecutado debido a error en la transacción");
                throw;
            }
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex,
                "❌ [REPOSITORY] Error Oracle en transacción. Proceso: {Proceso}, IdCarga: {IdCarga}, ErrorCode: {ErrorCode}",
                entidad.Proceso, entidad.IdCarga, ex.Number);
            throw new DataAccessException("Error al crear el registro de carga en la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> ActualizarContadoresDealersAsync(
        int cargaArchivoSincronizacionId,
        int dealersSincronizados,
        decimal porcDealersSinc,
        string usuarioModificacion,
        System.Data.IDbTransaction transaction)
    {
        const string sqlUpdate = @"
            UPDATE CO_CARGAARCHIVOSINCRONIZACION
            SET 
                COCA_DEALERSSONCRONIZADOS = :DealersSincronizados,
                COCA_PORCDEALERSSINC = :PorcDealersSinc,
                FECHAMODIFICACION = SYSDATE,
                USUARIOMODIFICACION = :UsuarioModificacion
            WHERE COCA_CARGAARCHIVOSINID = :CargaArchivoSincronizacionId";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Actualizando contadores de dealers. ID: {Id}, DealersSincronizados: {DealersSinc}, PorcDealersSinc: {Porc}",
                cargaArchivoSincronizacionId, dealersSincronizados, porcDealersSinc);

            var parametros = new DynamicParameters();
            parametros.Add("CargaArchivoSincronizacionId", cargaArchivoSincronizacionId);
            parametros.Add("DealersSincronizados", dealersSincronizados);
            parametros.Add("PorcDealersSinc", porcDealersSinc);
            parametros.Add("UsuarioModificacion", usuarioModificacion);

            // Usar la conexión de la transacción
            var connection = (Oracle.ManagedDataAccess.Client.OracleConnection)transaction.Connection!;
            var filasAfectadas = await connection.ExecuteAsync(sqlUpdate, parametros, transaction);

            _logger.LogInformation(
                "✅ [REPOSITORY] Contadores actualizados exitosamente. ID: {Id}, Filas afectadas: {Filas}",
                cargaArchivoSincronizacionId, filasAfectadas);

            return filasAfectadas;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex,
                "❌ [REPOSITORY] Error Oracle al actualizar contadores. ID: {Id}, ErrorCode: {ErrorCode}",
                cargaArchivoSincronizacionId, ex.Number);
            throw new DataAccessException("Error al actualizar los contadores de dealers en la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> ActualizarDealersTotalesAsync(
        int cargaArchivoSincronizacionId,
        string usuarioModificacion)
    {
        const string sqlUpdate = @"
            UPDATE CO_CARGAARCHIVOSINCRONIZACION
            SET 
                COCA_DEALERSTOTALES = (
                    SELECT COUNT(DISTINCT COSA_DEALERBAC)
                    FROM CO_FOTODEALERPRODUCTOS
                    WHERE COFD_COCA_CARGAARCHIVOSINID = :CargaArchivoSincronizacionId
                ),
                FECHAMODIFICACION = SYSDATE,
                USUARIOMODIFICACION = :UsuarioModificacion
            WHERE COCA_CARGAARCHIVOSINID = :CargaArchivoSincronizacionId";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Actualizando DealersTotales. ID: {Id}, Usuario: {Usuario}",
                cargaArchivoSincronizacionId, usuarioModificacion);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parametros = new DynamicParameters();
            parametros.Add("CargaArchivoSincronizacionId", cargaArchivoSincronizacionId);
            parametros.Add("UsuarioModificacion", usuarioModificacion);

            var filasAfectadas = await connection.ExecuteAsync(sqlUpdate, parametros);

            _logger.LogInformation(
                "✅ [REPOSITORY] DealersTotales actualizado exitosamente. ID: {Id}, Filas afectadas: {Filas}",
                cargaArchivoSincronizacionId, filasAfectadas);

            return filasAfectadas;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex,
                "❌ [REPOSITORY] Error Oracle al actualizar DealersTotales. ID: {Id}, ErrorCode: {ErrorCode}",
                cargaArchivoSincronizacionId, ex.Number);
            throw new DataAccessException("Error al actualizar DealersTotales en la base de datos", ex);
        }
    }

    /// <summary>
    /// Mapea el resultado de la consulta a la entidad.
    /// </summary>
    private static CargaArchivoSincronizacion MapearAEntidad(CargaArchivoSincMap map)
    {
        return new CargaArchivoSincronizacion
        {
            CargaArchivoSincronizacionId = map.CargaArchivoSincronizacionId,
            Proceso = map.Proceso ?? string.Empty,
            NombreArchivo = map.NombreArchivo ?? string.Empty,
            FechaCarga = map.FechaCarga,
            IdCarga = map.IdCarga ?? string.Empty,
            Registros = map.Registros,
            Actual = map.Actual == 1,
            DealersTotales = map.DealersTotales,
            DealersSincronizados = map.DealersSincronizados,
            PorcDealersSinc = map.PorcDealersSinc,
            FechaAlta = map.FechaAlta,
            UsuarioAlta = map.UsuarioAlta ?? string.Empty,
            FechaModificacion = map.FechaModificacion,
            UsuarioModificacion = map.UsuarioModificacion
        };
    }

    /// <summary>
    /// Clase auxiliar para mapeo de Dapper (maneja el campo Actual como int).
    /// </summary>
    private class CargaArchivoSincMap
    {
        public int CargaArchivoSincronizacionId { get; set; }
        public string? Proceso { get; set; }
        public string? NombreArchivo { get; set; }
        public DateTime FechaCarga { get; set; }
        public string? IdCarga { get; set; }
        public int Registros { get; set; }
        public int Actual { get; set; } // Oracle NUMBER(1) se mapea como int
        public int DealersTotales { get; set; }
        public int? DealersSincronizados { get; set; }
        public decimal? PorcDealersSinc { get; set; }
        public DateTime FechaAlta { get; set; }
        public string? UsuarioAlta { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }
}

