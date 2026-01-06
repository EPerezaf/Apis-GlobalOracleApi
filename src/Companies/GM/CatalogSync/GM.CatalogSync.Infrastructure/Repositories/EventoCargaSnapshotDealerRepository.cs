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
/// Repository para acceso a datos de Evento de Carga Snapshot de Dealers usando Dapper.
/// Tabla: CO_EVENTOSCARGASNAPSHOTDEALERS
/// </summary>
public class EventoCargaSnapshotDealerRepository : IEventoCargaSnapshotDealerRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<EventoCargaSnapshotDealerRepository> _logger;

    private const string TABLA = "CO_EVENTOSCARGASNAPSHOTDEALERS";
    private const string SECUENCIA = "SEQ_CO_EVENTOSCARGASNAPSHOTDEALERS";

    public EventoCargaSnapshotDealerRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<EventoCargaSnapshotDealerRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EventoCargaSnapshotDealer?> ObtenerPorIdAsync(int id)
    {
        const string sql = @"
            SELECT 
                f.COSD_EVENTOCARGASNAPDEALERID as EventoCargaSnapshotDealerId,
                f.COSD_COCP_EVENTOCARGAPROCESOID as EventoCargaProcesoId,
                f.COSD_DEALERBAC as DealerBac,
                f.COSD_NOMBREDEALER as NombreDealer,
                f.COSD_RAZONSOCIALDEALER as RazonSocialDealer,
                f.COSD_DMS as Dms,
                f.COSD_FECHAREGISTRO as FechaRegistro,
                f.COSD_FECHAALTA as FechaAlta,
                f.COSD_USUARIOALTA as UsuarioAlta,
                f.COSD_FECHAMODIFICACION as FechaModificacion,
                f.COSD_USUARIOMODIFICACION as UsuarioModificacion,
                c.COCP_IDCARGA as IdCarga,
                c.COCP_PROCESO as ProcesoCarga,
                c.COCP_FECHACARGA as FechaCarga,
                s.COSC_FECHASINCRONIZACION as FechaSincronizacion,
                CASE 
                    WHEN s.COSC_FECHASINCRONIZACION IS NOT NULL AND c.COCP_FECHACARGA IS NOT NULL 
                    THEN ROUND((s.COSC_FECHASINCRONIZACION - c.COCP_FECHACARGA) * 24, 2)
                    ELSE NULL
                END as TiempoSincronizacionHoras
            FROM CO_EVENTOSCARGASNAPSHOTDEALERS f
            INNER JOIN CO_EVENTOSCARGAPROCESO c ON f.COSD_COCP_EVENTOCARGAPROCESOID = c.COCP_EVENTOCARGAPROCESOID
            LEFT JOIN CO_SINCRONIZACIONCARGAPROCESODEALER s ON f.COSD_DEALERBAC = s.COSC_DEALERBAC 
                AND f.COSD_COCP_EVENTOCARGAPROCESOID = s.COSC_COCP_EVENTOCARGAPROCESOID
            WHERE f.COSD_EVENTOCARGASNAPDEALERID = :Id";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo evento carga snapshot dealer por ID: {Id}", id);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<EventoCargaSnapshotDealerMap>(sql, new { Id = id });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] Evento carga snapshot dealer con ID {Id} no encontrado", id);
                return null;
            }

            // Mapear a entidad (sin los campos del JOIN)
            var entidad = new EventoCargaSnapshotDealer
            {
                EventoCargaSnapshotDealerId = resultado.EventoCargaSnapshotDealerId,
                EventoCargaProcesoId = resultado.EventoCargaProcesoId,
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

            _logger.LogInformation("✅ [REPOSITORY] Evento carga snapshot dealer con ID {Id} obtenido exitosamente", id);
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
    public async Task<EventoCargaSnapshotDealerMap?> ObtenerPorIdCompletoAsync(int id)
    {
        const string sql = @"
            SELECT 
                f.COSD_EVENTOCARGASNAPDEALERID as EventoCargaSnapshotDealerId,
                f.COSD_COCP_EVENTOCARGAPROCESOID as EventoCargaProcesoId,
                f.COSD_DEALERBAC as DealerBac,
                f.COSD_NOMBREDEALER as NombreDealer,
                f.COSD_RAZONSOCIALDEALER as RazonSocialDealer,
                f.COSD_DMS as Dms,
                f.COSD_FECHAREGISTRO as FechaRegistro,
                f.COSD_FECHAALTA as FechaAlta,
                f.COSD_USUARIOALTA as UsuarioAlta,
                f.COSD_FECHAMODIFICACION as FechaModificacion,
                f.COSD_USUARIOMODIFICACION as UsuarioModificacion,
                f.COSD_URLWEBHOOK as UrlWebhook,
                f.COSD_SECRETKEY as SecretKey,
                c.COCP_IDCARGA as IdCarga,
                c.COCP_PROCESO as ProcesoCarga,
                c.COCP_FECHACARGA as FechaCarga,
                s.COSC_FECHASINCRONIZACION as FechaSincronizacion,
                s.COSC_TOKENCONFIRMACION as TokenConfirmacion,
                CASE 
                    WHEN s.COSC_FECHASINCRONIZACION IS NOT NULL AND c.COCP_FECHACARGA IS NOT NULL 
                    THEN ROUND((s.COSC_FECHASINCRONIZACION - c.COCP_FECHACARGA) * 24, 2)
                    ELSE NULL
                END as TiempoSincronizacionHoras,
                CASE 
                    WHEN s.COSC_FECHASINCRONIZACION IS NOT NULL THEN 1
                    ELSE 0
                END as Sincronizado
            FROM CO_EVENTOSCARGASNAPSHOTDEALERS f
            INNER JOIN CO_EVENTOSCARGAPROCESO c ON f.COSD_COCP_EVENTOCARGAPROCESOID = c.COCP_EVENTOCARGAPROCESOID
            LEFT JOIN CO_SINCRONIZACIONCARGAPROCESODEALER s ON f.COSD_DEALERBAC = s.COSC_DEALERBAC 
                AND f.COSD_COCP_EVENTOCARGAPROCESOID = s.COSC_COCP_EVENTOCARGAPROCESOID
            WHERE f.COSD_EVENTOCARGASNAPDEALERID = :Id";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo evento carga snapshot dealer completo por ID: {Id}", id);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<EventoCargaSnapshotDealerMap>(sql, new { Id = id });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] Evento carga snapshot dealer con ID {Id} no encontrado", id);
                return null;
            }

            _logger.LogInformation("✅ [REPOSITORY] Evento carga snapshot dealer completo con ID {Id} obtenido exitosamente", id);
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
    public async Task<(List<EventoCargaSnapshotDealerMap> data, int totalRecords)> ObtenerTodosConFiltrosCompletoAsync(
        int? eventoCargaProcesoId = null,
        string? dealerBac = null,
        string? dms = null,
        int? sincronizado = null,
        int page = 1,
        int pageSize = 200)
    {
        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Obteniendo eventos carga snapshot dealers completos con filtros. EventoCargaProcesoId: {EventoCargaProcesoId}, DealerBac: {DealerBac}, DMS: {Dms}, Página: {Page}, PageSize: {PageSize}",
                eventoCargaProcesoId?.ToString() ?? "null",
                dealerBac ?? "null",
                dms ?? "null",
                page,
                pageSize);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (eventoCargaProcesoId.HasValue)
            {
                whereClause += " AND f.COSD_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";
                parameters.Add("EventoCargaProcesoId", eventoCargaProcesoId.Value);
            }

            if (!string.IsNullOrWhiteSpace(dealerBac))
            {
                whereClause += " AND UPPER(f.COSD_DEALERBAC) LIKE UPPER(:DealerBac)";
                parameters.Add("DealerBac", $"%{dealerBac}%");
            }

            if (!string.IsNullOrWhiteSpace(dms))
            {
                whereClause += " AND UPPER(f.COSD_DMS) LIKE UPPER(:Dms)";
                parameters.Add("Dms", $"%{dms}%");
            }

            // Filtro por sincronizado (0 = no sincronizado, 1 = sincronizado)
            if (sincronizado.HasValue)
            {
                if (sincronizado.Value == 0)
                {
                    // No sincronizado: fechaSincronizacion IS NULL
                    whereClause += " AND s.COSC_FECHASINCRONIZACION IS NULL";
                }
                else if (sincronizado.Value == 1)
                {
                    // Sincronizado: fechaSincronizacion IS NOT NULL
                    whereClause += " AND s.COSC_FECHASINCRONIZACION IS NOT NULL";
                }
            }

            // Obtener total de registros (necesita incluir los JOINs para el filtro de sincronizado)
            var countSql = $@"
                SELECT COUNT(*) 
                FROM {TABLA} f
                INNER JOIN CO_EVENTOSCARGAPROCESO c ON f.COSD_COCP_EVENTOCARGAPROCESOID = c.COCP_EVENTOCARGAPROCESOID
                LEFT JOIN CO_SINCRONIZACIONCARGAPROCESODEALER s ON f.COSD_DEALERBAC = s.COSC_DEALERBAC 
                    AND f.COSD_COCP_EVENTOCARGAPROCESOID = s.COSC_COCP_EVENTOCARGAPROCESOID
                {whereClause}";
            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            if (totalRecords == 0)
            {
                _logger.LogInformation("ℹ️ [REPOSITORY] No se encontraron registros con los filtros especificados");
                return (new List<EventoCargaSnapshotDealerMap>(), 0);
            }

            // Aplicar paginación
            int offset = (page - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", offset + pageSize);

            var sql = $@"
                SELECT * FROM (
                    SELECT 
                        f.COSD_EVENTOCARGASNAPDEALERID as EventoCargaSnapshotDealerId,
                        f.COSD_COCP_EVENTOCARGAPROCESOID as EventoCargaProcesoId,
                        f.COSD_DEALERBAC as DealerBac,
                        f.COSD_NOMBREDEALER as NombreDealer,
                        f.COSD_RAZONSOCIALDEALER as RazonSocialDealer,
                        f.COSD_DMS as Dms,
                        f.COSD_FECHAREGISTRO as FechaRegistro,
                        f.COSD_FECHAALTA as FechaAlta,
                        f.COSD_USUARIOALTA as UsuarioAlta,
                        f.COSD_FECHAMODIFICACION as FechaModificacion,
                        f.COSD_USUARIOMODIFICACION as UsuarioModificacion,
                        f.COSD_URLWEBHOOK as UrlWebhook,
                        f.COSD_SECRETKEY as SecretKey,
                        c.COCP_IDCARGA as IdCarga,
                        c.COCP_PROCESO as ProcesoCarga,
                        c.COCP_FECHACARGA as FechaCarga,
                        s.COSC_FECHASINCRONIZACION as FechaSincronizacion,
                        s.COSC_TOKENCONFIRMACION as TokenConfirmacion,
                        CASE 
                            WHEN s.COSC_FECHASINCRONIZACION IS NOT NULL AND c.COCP_FECHACARGA IS NOT NULL 
                            THEN ROUND((s.COSC_FECHASINCRONIZACION - c.COCP_FECHACARGA) * 24, 2)
                            ELSE NULL
                        END as TiempoSincronizacionHoras,
                        CASE 
                            WHEN s.COSC_FECHASINCRONIZACION IS NOT NULL THEN 1
                            ELSE 0
                        END as Sincronizado,
                        ROW_NUMBER() OVER (ORDER BY f.COSD_EVENTOCARGASNAPDEALERID DESC) AS RNUM
                    FROM {TABLA} f
                    INNER JOIN CO_EVENTOSCARGAPROCESO c ON f.COSD_COCP_EVENTOCARGAPROCESOID = c.COCP_EVENTOCARGAPROCESOID
                    LEFT JOIN CO_SINCRONIZACIONCARGAPROCESODEALER s ON f.COSD_DEALERBAC = s.COSC_DEALERBAC 
                        AND f.COSD_COCP_EVENTOCARGAPROCESOID = s.COSC_COCP_EVENTOCARGAPROCESOID
                    {whereClause}
                ) WHERE RNUM > :offset AND RNUM <= :limit";

            var resultados = await connection.QueryAsync<EventoCargaSnapshotDealerMap>(sql, parameters);
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
    public async Task<bool> ExisteEventoCargaProcesoIdAsync(int eventoCargaProcesoId)
    {
        const string sql = @"
            SELECT COUNT(*) 
            FROM CO_EVENTOSCARGAPROCESO 
            WHERE COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Verificando existencia de EventoCargaProcesoId: {Id}", eventoCargaProcesoId);

            using var connection = await _connectionFactory.CreateConnectionAsync();
            var count = await connection.ExecuteScalarAsync<int>(sql, new { EventoCargaProcesoId = eventoCargaProcesoId });

            var existe = count > 0;
            _logger.LogInformation("✅ [REPOSITORY] EventoCargaProcesoId {Id} existe: {Existe}", eventoCargaProcesoId, existe);
            return existe;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al verificar existencia de EventoCargaProcesoId {Id}. ErrorCode: {ErrorCode}",
                eventoCargaProcesoId, ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<(List<EventoCargaSnapshotDealer> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        int? eventoCargaProcesoId = null,
        string? dealerBac = null,
        string? dms = null,
        int page = 1,
        int pageSize = 200)
    {
        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Consultando eventos carga snapshot dealers - EventoCargaProcesoId: {EventoCargaProcesoId}, DealerBac: {DealerBac}, DMS: {Dms}, Página: {Page}, PageSize: {PageSize}",
                eventoCargaProcesoId?.ToString() ?? "Todos",
                dealerBac ?? "Todos",
                dms ?? "Todos",
                page,
                pageSize);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (eventoCargaProcesoId.HasValue)
            {
                whereClause += " AND COSD_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";
                parameters.Add("EventoCargaProcesoId", eventoCargaProcesoId.Value);
            }

            if (!string.IsNullOrWhiteSpace(dealerBac))
            {
                whereClause += " AND UPPER(COSD_DEALERBAC) LIKE UPPER(:DealerBac)";
                parameters.Add("DealerBac", $"%{dealerBac}%");
            }

            if (!string.IsNullOrWhiteSpace(dms))
            {
                whereClause += " AND UPPER(COSD_DMS) LIKE UPPER(:Dms)";
                parameters.Add("Dms", $"%{dms}%");
            }

            // Obtener total de registros
            var countSql = $"SELECT COUNT(*) FROM {TABLA} {whereClause}";
            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            if (totalRecords == 0)
            {
                _logger.LogInformation("ℹ️ [REPOSITORY] No se encontraron registros con los filtros especificados");
                return (new List<EventoCargaSnapshotDealer>(), 0);
            }

            // Aplicar paginación
            int offset = (page - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", offset + pageSize);

            var sql = $@"
                SELECT * FROM (
                    SELECT 
                        COSD_EVENTOCARGASNAPDEALERID as EventoCargaSnapshotDealerId,
                        COSD_COCP_EVENTOCARGAPROCESOID as EventoCargaProcesoId,
                        COSD_DEALERBAC as DealerBac,
                        COSD_NOMBREDEALER as NombreDealer,
                        COSD_RAZONSOCIALDEALER as RazonSocialDealer,
                        COSD_DMS as Dms,
                        COSD_FECHAREGISTRO as FechaRegistro,
                        COSD_FECHAALTA as FechaAlta,
                        COSD_USUARIOALTA as UsuarioAlta,
                        COSD_FECHAMODIFICACION as FechaModificacion,
                        COSD_USUARIOMODIFICACION as UsuarioModificacion,
                        COSD_URLWEBHOOK as UrlWebhook,
                        COSD_SECRETKEY as SecretKey,
                        ROW_NUMBER() OVER (ORDER BY COSD_EVENTOCARGASNAPDEALERID DESC) AS RNUM
                    FROM {TABLA}
                    {whereClause}
                ) WHERE RNUM > :offset AND RNUM <= :limit";

            var resultados = await connection.QueryAsync<EventoCargaSnapshotDealer>(sql, parameters);
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
    public async Task<bool> ExisteCombinacionAsync(int eventoCargaProcesoId, string dealerBac)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM CO_EVENTOSCARGASNAPSHOTDEALERS
            WHERE COSD_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId
            AND COSD_DEALERBAC = :DealerBac";

        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            var count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                EventoCargaProcesoId = eventoCargaProcesoId,
                DealerBac = dealerBac
            });

            return count > 0;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex,
                "❌ [REPOSITORY] Error Oracle al verificar combinación. EventoCargaProcesoId: {EventoCargaProcesoId}, DealerBac: {DealerBac}, ErrorCode: {ErrorCode}",
                eventoCargaProcesoId, dealerBac, ex.Number);
            throw new DataAccessException("Error al verificar la existencia del registro en la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<List<EventoCargaSnapshotDealer>> ObtenerPorEventoCargaProcesoIdAsync(int eventoCargaProcesoId)
    {
        const string sql = @"
            SELECT 
                COSD_EVENTOCARGASNAPDEALERID as EventoCargaSnapshotDealerId,
                COSD_COCP_EVENTOCARGAPROCESOID as EventoCargaProcesoId,
                COSD_DEALERBAC as DealerBac,
                COSD_NOMBREDEALER as NombreDealer,
                COSD_RAZONSOCIALDEALER as RazonSocialDealer,
                COSD_DMS as Dms,
                COSD_FECHAREGISTRO as FechaRegistro,
                COSD_FECHAALTA as FechaAlta,
                COSD_USUARIOALTA as UsuarioAlta,
                COSD_FECHAMODIFICACION as FechaModificacion,
                COSD_USUARIOMODIFICACION as UsuarioModificacion,
                COSD_URLWEBHOOK as UrlWebhook,
                COSD_SECRETKEY as SecretKey
            FROM CO_EVENTOSCARGASNAPSHOTDEALERS
            WHERE COSD_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo eventos carga snapshot dealers por EventoCargaProcesoId: {Id}", eventoCargaProcesoId);

            using var connection = await _connectionFactory.CreateConnectionAsync();
            var resultados = await connection.QueryAsync<EventoCargaSnapshotDealer>(sql, new { EventoCargaProcesoId = eventoCargaProcesoId });
            var lista = resultados.ToList();

            _logger.LogInformation("✅ [REPOSITORY] Se obtuvieron {Cantidad} registros para EventoCargaProcesoId {Id}", lista.Count, eventoCargaProcesoId);
            return lista;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al obtener registros por EventoCargaProcesoId {Id}. ErrorCode: {ErrorCode}",
                eventoCargaProcesoId, ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<List<EventoCargaSnapshotDealer>> CrearBatchAsync(
        List<EventoCargaSnapshotDealer> entidades,
        string usuarioAlta)
    {
        if (entidades == null || !entidades.Any())
        {
            throw new BusinessValidationException("La lista de entidades no puede estar vacía", new List<ValidationError>());
        }

        var sqlInsert = $@"
            INSERT INTO CO_EVENTOSCARGASNAPSHOTDEALERS (
                COSD_EVENTOCARGASNAPDEALERID,
                COSD_COCP_EVENTOCARGAPROCESOID,
                COSD_DEALERBAC,
                COSD_NOMBREDEALER,
                COSD_RAZONSOCIALDEALER,
                COSD_DMS,
                COSD_FECHAREGISTRO,
                COSD_FECHAALTA,
                COSD_USUARIOALTA,
                COSD_URLWEBHOOK,
                COSD_SECRETKEY
            ) VALUES (
                {SECUENCIA}.NEXTVAL,
                :EventoCargaProcesoId,
                :DealerBac,
                :NombreDealer,
                :RazonSocialDealer,
                :Dms,
                :FechaRegistro,
                SYSDATE,
                :UsuarioAlta,
                :UrlWebhook,
                :SecretKey
            ) RETURNING COSD_EVENTOCARGASNAPDEALERID INTO :Id";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Iniciando inserción batch de {Cantidad} registros. Usuario: {Usuario}",
                entidades.Count, usuarioAlta);

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var entidadesCreadas = new List<EventoCargaSnapshotDealer>();

                foreach (var entidad in entidades)
                {
                    // Validar que los campos requeridos no sean NULL
                    if (string.IsNullOrWhiteSpace(entidad.NombreDealer))
                    {
                        _logger.LogWarning("⚠️ [REPOSITORY] NombreDealer es NULL o vacío para DealerBac: {DealerBac}. Usando DealerBac como fallback.",
                            entidad.DealerBac);
                        entidad.NombreDealer = entidad.DealerBac ?? "SIN NOMBRE";
                    }

                    if (string.IsNullOrWhiteSpace(entidad.RazonSocialDealer))
                    {
                        _logger.LogWarning("⚠️ [REPOSITORY] RazonSocialDealer es NULL o vacío para DealerBac: {DealerBac}. Usando NombreDealer como fallback.",
                            entidad.DealerBac);
                        entidad.RazonSocialDealer = entidad.NombreDealer ?? entidad.DealerBac ?? "SIN RAZON SOCIAL";
                    }

                    var parametros = new DynamicParameters();
                    parametros.Add("EventoCargaProcesoId", entidad.EventoCargaProcesoId);
                    parametros.Add("DealerBac", entidad.DealerBac ?? string.Empty);
                    parametros.Add("NombreDealer", entidad.NombreDealer ?? string.Empty);
                    parametros.Add("RazonSocialDealer", entidad.RazonSocialDealer ?? string.Empty);
                    parametros.Add("Dms", entidad.Dms ?? "GDMS");
                    parametros.Add("FechaRegistro", entidad.FechaRegistro);
                    parametros.Add("UsuarioAlta", usuarioAlta);
                    parametros.Add("UrlWebhook", entidad.UrlWebhook);
                    parametros.Add("SecretKey", entidad.SecretKey);
                    parametros.Add("Id", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

                    await connection.ExecuteAsync(sqlInsert, parametros, transaction);

                    var nuevoId = parametros.Get<int>("Id");
                    entidad.EventoCargaSnapshotDealerId = nuevoId;
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

