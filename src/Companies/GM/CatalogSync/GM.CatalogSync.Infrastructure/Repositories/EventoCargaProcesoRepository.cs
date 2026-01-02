using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.CatalogSync.Infrastructure.Repositories;

/// <summary>
/// Repository para acceso a datos de Evento de Carga de Proceso usando Dapper.
/// Tabla: CO_EVENTOSCARGAPROCESO
/// </summary>
public class EventoCargaProcesoRepository : IEventoCargaProcesoRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<EventoCargaProcesoRepository> _logger;

    private const string TABLA = "CO_EVENTOSCARGAPROCESO";
    private const string SECUENCIA = "SEQ_CO_EVENTOSCARGAPROCESO";

    public EventoCargaProcesoRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<EventoCargaProcesoRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EventoCargaProceso?> ObtenerPorIdAsync(int id)
    {
        const string sql = @"
            SELECT 
                COCP_EVENTOCARGAPROCESOID as EventoCargaProcesoId,
                COCP_PROCESO as Proceso,
                COCP_NOMBREARCHIVO as NombreArchivo,
                COCP_FECHACARGA as FechaCarga,
                COCP_IDCARGA as IdCarga,
                COCP_REGISTROS as Registros,
                COCP_ACTUAL as Actual,
                COCP_DEALERSTOTALES as DealersTotales,
                COCP_DEALERSSINCRONIZADOS as DealersSincronizados,
                COCP_PORCDEALERSSINC as PorcDealersSinc,
                COCP_TABLARELACION as TablaRelacion,
                COCP_COMPONENTERELACIONADO as ComponenteRelacionado,
                COCP_FECHAALTA as FechaAlta,
                COCP_USUARIOALTA as UsuarioAlta,
                COCP_FECHAMODIFICACION as FechaModificacion,
                COCP_USUARIOMODIFICACION as UsuarioModificacion
            FROM CO_EVENTOSCARGAPROCESO
            WHERE COCP_EVENTOCARGAPROCESOID = :Id";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo registro de evento de carga por ID: {Id}", id);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<EventoCargaProcesoMap>(sql, new { Id = id });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] Registro de evento de carga con ID {Id} no encontrado", id);
                return null;
            }

            _logger.LogInformation("✅ [REPOSITORY] Registro de evento de carga con ID {Id} obtenido exitosamente", id);
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
    public async Task<(List<EventoCargaProceso> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        string? idCarga = null,
        bool? actual = null,
        int page = 1,
        int pageSize = 200)
    {
        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Consultando registros de evento de carga - Proceso: {Proceso}, IdCarga: {IdCarga}, Actual: {Actual}, Página: {Page}, PageSize: {PageSize}",
                proceso ?? "Todos", idCarga ?? "Todos", actual?.ToString() ?? "Todos", page, pageSize);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(proceso))
            {
                whereClause += " AND UPPER(COCP_PROCESO) LIKE UPPER(:Proceso)";
                parameters.Add("Proceso", $"%{proceso}%");
            }

            if (!string.IsNullOrWhiteSpace(idCarga))
            {
                whereClause += " AND UPPER(COCP_IDCARGA) LIKE UPPER(:IdCarga)";
                parameters.Add("IdCarga", $"%{idCarga}%");
            }

            if (actual.HasValue)
            {
                whereClause += " AND COCP_ACTUAL = :Actual";
                parameters.Add("Actual", actual.Value ? 1 : 0);
            }

            // Obtener total de registros
            var countSql = $"SELECT COUNT(*) FROM {TABLA} {whereClause}";
            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            if (totalRecords == 0)
            {
                _logger.LogInformation("✅ [REPOSITORY] No se encontraron registros de evento de carga");
                return (new List<EventoCargaProceso>(), 0);
            }

            // Aplicar paginación
            int offset = (page - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", offset + pageSize);

            var sql = $@"
                SELECT * FROM (
                    SELECT 
                        COCP_EVENTOCARGAPROCESOID as EventoCargaProcesoId,
                        COCP_PROCESO as Proceso,
                        COCP_NOMBREARCHIVO as NombreArchivo,
                        COCP_FECHACARGA as FechaCarga,
                        COCP_IDCARGA as IdCarga,
                        COCP_REGISTROS as Registros,
                        COCP_ACTUAL as Actual,
                        COCP_DEALERSTOTALES as DealersTotales,
                        COCP_DEALERSSINCRONIZADOS as DealersSincronizados,
                        COCP_PORCDEALERSSINC as PorcDealersSinc,
                        COCP_TABLARELACION as TablaRelacion,
                        COCP_COMPONENTERELACIONADO as ComponenteRelacionado,
                        COCP_FECHAALTA as FechaAlta,
                        COCP_USUARIOALTA as UsuarioAlta,
                        COCP_FECHAMODIFICACION as FechaModificacion,
                        COCP_USUARIOMODIFICACION as UsuarioModificacion,
                        ROW_NUMBER() OVER (ORDER BY COCP_FECHACARGA DESC) AS RNUM
                    FROM {TABLA}
                    {whereClause}
                ) WHERE RNUM > :offset AND RNUM <= :limit";

            var resultados = await connection.QueryAsync<EventoCargaProcesoMap>(sql, parameters);
            var lista = resultados.Select(MapearAEntidad).ToList();

            _logger.LogInformation("✅ [REPOSITORY] Se obtuvieron {Cantidad} registros de evento de carga de {Total} totales (Página {Page})", 
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
            FROM CO_EVENTOSCARGAPROCESO 
            WHERE COCP_IDCARGA = :IdCarga";

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
    public async Task<EventoCargaProceso> CrearConTransaccionAsync(
        EventoCargaProceso entidad,
        string usuarioAlta)
    {
        const string sqlUpdate = @"
            UPDATE CO_EVENTOSCARGAPROCESO 
            SET COCP_ACTUAL = 0,
                COCP_FECHAMODIFICACION = SYSDATE,
                COCP_USUARIOMODIFICACION = :UsuarioModificacion
            WHERE COCP_PROCESO = :Proceso 
            AND COCP_ACTUAL = 1";

        const string sqlInsert = @"
            INSERT INTO CO_EVENTOSCARGAPROCESO (
                COCP_EVENTOCARGAPROCESOID,
                COCP_PROCESO,
                COCP_NOMBREARCHIVO,
                COCP_FECHACARGA,
                COCP_IDCARGA,
                COCP_REGISTROS,
                COCP_ACTUAL,
                COCP_DEALERSTOTALES,
                COCP_DEALERSSINCRONIZADOS,
                COCP_PORCDEALERSSINC,
                COCP_TABLARELACION,
                COCP_COMPONENTERELACIONADO,
                COCP_FECHAALTA,
                COCP_USUARIOALTA
            ) VALUES (
                :Id,
                :Proceso,
                :NombreArchivo,
                :FechaCarga,
                :IdCarga,
                :Registros,
                1,
                :DealersTotales,
                0,
                0.00,
                :TablaRelacion,
                :ComponenteRelacionado,
                SYSDATE,
                :UsuarioAlta
            )";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Iniciando transacción para crear registro de evento de carga. Proceso: {Proceso}, IdCarga: {IdCarga}, Usuario: {Usuario}",
                entidad.Proceso, entidad.IdCarga, usuarioAlta);

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Actualizar registros anteriores del mismo proceso a COCP_ACTUAL = 0
                var registrosActualizados = await connection.ExecuteAsync(
                    sqlUpdate,
                    new
                    {
                        Proceso = entidad.Proceso,
                        UsuarioModificacion = usuarioAlta
                    },
                    transaction);

                _logger.LogInformation(
                    "📝 [REPOSITORY] {Cantidad} registros anteriores actualizados a COCP_ACTUAL=0 para proceso: {Proceso}",
                    registrosActualizados, entidad.Proceso);

                // 2. Obtener el siguiente ID de la secuencia
                var sqlGetNextId = $"SELECT {SECUENCIA}.NEXTVAL FROM DUAL";
                var nuevoId = await connection.ExecuteScalarAsync<int>(sqlGetNextId, null, transaction);

                _logger.LogInformation(
                    "🔢 [REPOSITORY] ID obtenido de la secuencia: {Id}",
                    nuevoId);

                // 3. Insertar nuevo registro con COCP_ACTUAL = 1
                var parametrosInsert = new DynamicParameters();
                parametrosInsert.Add("Id", nuevoId);
                parametrosInsert.Add("Proceso", entidad.Proceso);
                parametrosInsert.Add("NombreArchivo", entidad.NombreArchivo);
                parametrosInsert.Add("FechaCarga", entidad.FechaCarga);
                parametrosInsert.Add("IdCarga", entidad.IdCarga);
                parametrosInsert.Add("Registros", entidad.Registros);
                parametrosInsert.Add("DealersTotales", entidad.DealersTotales);
                parametrosInsert.Add("TablaRelacion", entidad.TablaRelacion);
                parametrosInsert.Add("ComponenteRelacionado", entidad.ComponenteRelacionado);
                parametrosInsert.Add("UsuarioAlta", usuarioAlta);

                var filasInsertadas = await connection.ExecuteAsync(sqlInsert, parametrosInsert, transaction);

                _logger.LogInformation(
                    "📝 [REPOSITORY] Registro insertado exitosamente. ID: {Id}, Filas afectadas: {Filas}",
                    nuevoId, filasInsertadas);

                // 4. Commit de la transacción
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
            
            // Mensaje más descriptivo para errores de secuencia
            if (ex.Number == 2289) // ORA-02289: no existe la secuencia
            {
                var mensaje = $"La secuencia '{SECUENCIA}' no existe en la base de datos. " +
                             $"Por favor, ejecute el siguiente script SQL para crear la secuencia:\n\n" +
                             $"CREATE SEQUENCE {SECUENCIA}\n" +
                             $"START WITH 1\n" +
                             $"INCREMENT BY 1\n" +
                             $"NOCACHE\n" +
                             $"NOCYCLE;";
                throw new DataAccessException(mensaje, ex);
            }
            
            throw new DataAccessException("Error al crear el registro de evento de carga en la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> ActualizarContadoresDealersAsync(
        int eventoCargaProcesoId,
        int dealersSincronizados,
        decimal porcDealersSinc,
        string usuarioModificacion,
        System.Data.IDbTransaction transaction)
    {
        const string sqlUpdate = @"
            UPDATE CO_EVENTOSCARGAPROCESO
            SET 
                COCP_DEALERSSINCRONIZADOS = :DealersSincronizados,
                COCP_PORCDEALERSSINC = :PorcDealersSinc,
                COCP_FECHAMODIFICACION = SYSDATE,
                COCP_USUARIOMODIFICACION = :UsuarioModificacion
            WHERE COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Actualizando contadores de dealers. ID: {Id}, DealersSincronizados: {DealersSinc}, PorcDealersSinc: {Porc}",
                eventoCargaProcesoId, dealersSincronizados, porcDealersSinc);

            var parametros = new DynamicParameters();
            parametros.Add("EventoCargaProcesoId", eventoCargaProcesoId);
            parametros.Add("DealersSincronizados", dealersSincronizados);
            parametros.Add("PorcDealersSinc", porcDealersSinc);
            parametros.Add("UsuarioModificacion", usuarioModificacion);

            // Usar la conexión de la transacción
            var connection = (Oracle.ManagedDataAccess.Client.OracleConnection)transaction.Connection!;
            var filasAfectadas = await connection.ExecuteAsync(sqlUpdate, parametros, transaction);

            _logger.LogInformation(
                "✅ [REPOSITORY] Contadores actualizados exitosamente. ID: {Id}, Filas afectadas: {Filas}",
                eventoCargaProcesoId, filasAfectadas);

            return filasAfectadas;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex,
                "❌ [REPOSITORY] Error Oracle al actualizar contadores. ID: {Id}, ErrorCode: {ErrorCode}",
                eventoCargaProcesoId, ex.Number);
            throw new DataAccessException("Error al actualizar los contadores de dealers en la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> ActualizarDealersTotalesAsync(
        int eventoCargaProcesoId,
        string usuarioModificacion)
    {
        const string sqlUpdate = @"
            UPDATE CO_EVENTOSCARGAPROCESO
            SET 
                COCP_DEALERSTOTALES = (
                    SELECT COUNT(DISTINCT COSD_DEALERBAC)
                    FROM CO_EVENTOSCARGASNAPSHOTDEALERS
                    WHERE COSD_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId
                ),
                COCP_FECHAMODIFICACION = SYSDATE,
                COCP_USUARIOMODIFICACION = :UsuarioModificacion
            WHERE COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Actualizando DealersTotales. ID: {Id}, Usuario: {Usuario}",
                eventoCargaProcesoId, usuarioModificacion);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parametros = new DynamicParameters();
            parametros.Add("EventoCargaProcesoId", eventoCargaProcesoId);
            parametros.Add("UsuarioModificacion", usuarioModificacion);

            var filasAfectadas = await connection.ExecuteAsync(sqlUpdate, parametros);

            _logger.LogInformation(
                "✅ [REPOSITORY] DealersTotales actualizado exitosamente. ID: {Id}, Filas afectadas: {Filas}",
                eventoCargaProcesoId, filasAfectadas);

            return filasAfectadas;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex,
                "❌ [REPOSITORY] Error Oracle al actualizar DealersTotales. ID: {Id}, ErrorCode: {ErrorCode}",
                eventoCargaProcesoId, ex.Number);
            throw new DataAccessException("Error al actualizar DealersTotales en la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> ActualizarDealersTotalesAsync(
        int eventoCargaProcesoId,
        string usuarioModificacion,
        System.Data.IDbTransaction transaction)
    {
        const string sqlUpdate = @"
            UPDATE CO_EVENTOSCARGAPROCESO
            SET 
                COCP_DEALERSTOTALES = (
                    SELECT COUNT(DISTINCT COSD_DEALERBAC)
                    FROM CO_EVENTOSCARGASNAPSHOTDEALERS
                    WHERE COSD_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId
                ),
                COCP_FECHAMODIFICACION = SYSDATE,
                COCP_USUARIOMODIFICACION = :UsuarioModificacion
            WHERE COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Actualizando DealersTotales en transacción. ID: {Id}, Usuario: {Usuario}",
                eventoCargaProcesoId, usuarioModificacion);

            var parametros = new DynamicParameters();
            parametros.Add("EventoCargaProcesoId", eventoCargaProcesoId);
            parametros.Add("UsuarioModificacion", usuarioModificacion);

            // Usar la conexión de la transacción
            var connection = (Oracle.ManagedDataAccess.Client.OracleConnection)transaction.Connection!;
            var filasAfectadas = await connection.ExecuteAsync(sqlUpdate, parametros, transaction);

            _logger.LogInformation(
                "✅ [REPOSITORY] DealersTotales actualizado exitosamente en transacción. ID: {Id}, Filas afectadas: {Filas}",
                eventoCargaProcesoId, filasAfectadas);

            return filasAfectadas;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex,
                "❌ [REPOSITORY] Error Oracle al actualizar DealersTotales en transacción. ID: {Id}, ErrorCode: {ErrorCode}",
                eventoCargaProcesoId, ex.Number);
            throw new DataAccessException("Error al actualizar DealersTotales en la base de datos", ex);
        }
    }

    /// <summary>
    /// Mapea el resultado de la consulta a la entidad.
    /// </summary>
    private static EventoCargaProceso MapearAEntidad(EventoCargaProcesoMap map)
    {
        return new EventoCargaProceso
        {
            EventoCargaProcesoId = map.EventoCargaProcesoId,
            Proceso = map.Proceso ?? string.Empty,
            NombreArchivo = map.NombreArchivo ?? string.Empty,
            FechaCarga = map.FechaCarga,
            IdCarga = map.IdCarga ?? string.Empty,
            Registros = map.Registros,
            Actual = map.Actual == 1,
            DealersTotales = map.DealersTotales,
            DealersSincronizados = map.DealersSincronizados,
            PorcDealersSinc = map.PorcDealersSinc,
            TablaRelacion = map.TablaRelacion,
            ComponenteRelacionado = map.ComponenteRelacionado,
            FechaAlta = map.FechaAlta,
            UsuarioAlta = map.UsuarioAlta ?? string.Empty,
            FechaModificacion = map.FechaModificacion,
            UsuarioModificacion = map.UsuarioModificacion
        };
    }

    /// <summary>
    /// Clase auxiliar para mapeo de Dapper (maneja el campo Actual como int).
    /// </summary>
    private class EventoCargaProcesoMap
    {
        public int EventoCargaProcesoId { get; set; }
        public string? Proceso { get; set; }
        public string? NombreArchivo { get; set; }
        public DateTime FechaCarga { get; set; }
        public string? IdCarga { get; set; }
        public int Registros { get; set; }
        public int Actual { get; set; } // Oracle NUMBER(1) se mapea como int
        public int DealersTotales { get; set; }
        public int? DealersSincronizados { get; set; }
        public decimal? PorcDealersSinc { get; set; }
        public string? TablaRelacion { get; set; }
        public string? ComponenteRelacionado { get; set; }
        public DateTime FechaAlta { get; set; }
        public string? UsuarioAlta { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }
}

