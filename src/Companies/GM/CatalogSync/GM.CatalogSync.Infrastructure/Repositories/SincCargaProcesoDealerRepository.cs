using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.CatalogSync.Infrastructure.Repositories;

/// <summary>
/// Clase auxiliar para mapear información de evento de carga de proceso.
/// </summary>
internal class EventoCargaProcesoInfo
{
    public int EventoCargaProcesoId { get; set; }
    public int DealersTotales { get; set; }
}

/// <summary>
/// Clase auxiliar para mapear resultados de JOIN con CO_EVENTOSCARGAPROCESO.
/// </summary>
internal class SincCargaProcesoDealerMap
{
    public int SincCargaProcesoDealerId { get; set; }
    public string Proceso { get; set; } = string.Empty;
    public int EventoCargaProcesoId { get; set; }
    public string DmsOrigen { get; set; } = string.Empty;
    public string DealerBac { get; set; } = string.Empty;
    public string NombreDealer { get; set; } = string.Empty;
    public DateTime FechaSincronizacion { get; set; }
    public int RegistrosSincronizados { get; set; }
    public DateTime FechaAlta { get; set; }
    public string UsuarioAlta { get; set; } = string.Empty;
    public DateTime? FechaModificacion { get; set; }
    public string? UsuarioModificacion { get; set; }
    public string? IdCarga { get; set; }
    public string? ProcesoCarga { get; set; }
    public DateTime? FechaCarga { get; set; }
    public decimal? TiempoSincronizacionHoras { get; set; }
    public string? TokenConfirmacion { get; set; }
}

/// <summary>
/// Repository para acceso a datos de Sincronización de Carga de Proceso por Dealer usando Dapper.
/// Tabla: CO_SINCRONIZACIONCARGAPROCESODEALER
/// </summary>
public class SincCargaProcesoDealerRepository : ISincCargaProcesoDealerRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly IEventoCargaProcesoRepository _eventoCargaProcesoRepository;
    private readonly ILogger<SincCargaProcesoDealerRepository> _logger;

    private const string TABLA = "CO_SINCRONIZACIONCARGAPROCESODEALER";
    private const string SECUENCIA = "SEQ_CO_SINCRONIZACIONCARGAPROCESODEALER";

    public SincCargaProcesoDealerRepository(
        IOracleConnectionFactory connectionFactory,
        IEventoCargaProcesoRepository eventoCargaProcesoRepository,
        ILogger<SincCargaProcesoDealerRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _eventoCargaProcesoRepository = eventoCargaProcesoRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SincCargaProcesoDealer?> ObtenerPorIdAsync(int id)
    {
        const string sql = @"
            SELECT 
                s.COSC_SINCARGAPROCESODEALERID as SincCargaProcesoDealerId,
                c.COCP_PROCESO as Proceso,
                s.COSC_COCP_EVENTOCARGAPROCESOID as EventoCargaProcesoId,
                s.COSC_DMSORIGEN as DmsOrigen,
                s.COSC_DEALERBAC as DealerBac,
                s.COSC_NOMBREDEALER as NombreDealer,
                s.COSC_FECHASINCRONIZACION as FechaSincronizacion,
                s.COSC_REGISTROSSINCRONIZADOS as RegistrosSincronizados,
                s.COSC_TOKENCONFIRMACION as TokenConfirmacion,
                s.COSC_FECHAALTA as FechaAlta,
                s.COSC_USUARIOALTA as UsuarioAlta,
                s.COSC_FECHAMODIFICACION as FechaModificacion,
                s.COSC_USUARIOMODIFICACION as UsuarioModificacion,
                c.COCP_IDCARGA as IdCarga,
                c.COCP_PROCESO as ProcesoCarga,
                c.COCP_FECHACARGA as FechaCarga,
                ROUND((s.COSC_FECHASINCRONIZACION - c.COCP_FECHACARGA) * 24, 2) as TiempoSincronizacionHoras
            FROM CO_SINCRONIZACIONCARGAPROCESODEALER s
            INNER JOIN CO_EVENTOSCARGAPROCESO c ON s.COSC_COCP_EVENTOCARGAPROCESOID = c.COCP_EVENTOCARGAPROCESOID
            WHERE s.COSC_SINCARGAPROCESODEALERID = :Id";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo registro de sincronización por ID: {Id}", id);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<SincCargaProcesoDealerMap>(sql, new { Id = id });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] Registro de sincronización con ID {Id} no encontrado", id);
                return null;
            }

            // Mapear a entidad (sin los campos del JOIN)
            var entidad = new SincCargaProcesoDealer
            {
                SincCargaProcesoDealerId = resultado.SincCargaProcesoDealerId,
                Proceso = resultado.Proceso,
                EventoCargaProcesoId = resultado.EventoCargaProcesoId,
                DmsOrigen = resultado.DmsOrigen,
                DealerBac = resultado.DealerBac,
                NombreDealer = resultado.NombreDealer,
                FechaSincronizacion = resultado.FechaSincronizacion,
                RegistrosSincronizados = resultado.RegistrosSincronizados,
                TokenConfirmacion = resultado.TokenConfirmacion ?? string.Empty,
                FechaAlta = resultado.FechaAlta,
                UsuarioAlta = resultado.UsuarioAlta,
                FechaModificacion = resultado.FechaModificacion,
                UsuarioModificacion = resultado.UsuarioModificacion
            };

            _logger.LogInformation("✅ [REPOSITORY] Registro de sincronización con ID {Id} obtenido exitosamente", id);
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
    public async Task<(List<SincCargaProcesoDealer> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        int? eventoCargaProcesoId = null,
        string? dealerBac = null,
        int page = 1,
        int pageSize = 200)
    {
        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Consultando registros de sincronización - Proceso: {Proceso}, EventoCargaProcesoId: {EventoCargaProcesoId}, DealerBac: {DealerBac}, Página: {Page}, PageSize: {PageSize}",
                proceso ?? "Todos", eventoCargaProcesoId?.ToString() ?? "Todos", dealerBac ?? "Todos", page, pageSize);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(proceso))
            {
                whereClause += " AND UPPER(c.COCP_PROCESO) LIKE UPPER(:Proceso)";
                parameters.Add("Proceso", $"%{proceso}%");
            }

            if (eventoCargaProcesoId.HasValue)
            {
                whereClause += " AND COSC_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";
                parameters.Add("EventoCargaProcesoId", eventoCargaProcesoId.Value);
            }

            if (!string.IsNullOrWhiteSpace(dealerBac))
            {
                whereClause += " AND UPPER(COSC_DEALERBAC) LIKE UPPER(:DealerBac)";
                parameters.Add("DealerBac", $"%{dealerBac}%");
            }

            // Obtener total de registros
            var countSql = $"SELECT COUNT(*) FROM {TABLA} {whereClause}";
            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            if (totalRecords == 0)
            {
                _logger.LogInformation("✅ [REPOSITORY] No se encontraron registros de sincronización");
                return (new List<SincCargaProcesoDealer>(), 0);
            }

            // Aplicar paginación
            int offset = (page - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", offset + pageSize);

            var sql = $@"
                SELECT * FROM (
                    SELECT 
                        s.COSC_SINCARGAPROCESODEALERID as SincCargaProcesoDealerId,
                        c.COCP_PROCESO as Proceso,
                        s.COSC_COCP_EVENTOCARGAPROCESOID as EventoCargaProcesoId,
                        s.COSC_DMSORIGEN as DmsOrigen,
                        s.COSC_DEALERBAC as DealerBac,
                        s.COSC_NOMBREDEALER as NombreDealer,
                        s.COSC_FECHASINCRONIZACION as FechaSincronizacion,
                        s.COSC_REGISTROSSINCRONIZADOS as RegistrosSincronizados,
                        s.COSC_TOKENCONFIRMACION as TokenConfirmacion,
                        s.COSC_FECHAALTA as FechaAlta,
                        s.COSC_USUARIOALTA as UsuarioAlta,
                        s.COSC_FECHAMODIFICACION as FechaModificacion,
                        s.COSC_USUARIOMODIFICACION as UsuarioModificacion,
                        c.COCP_IDCARGA as IdCarga,
                        c.COCP_PROCESO as ProcesoCarga,
                        c.COCP_FECHACARGA as FechaCarga,
                        ROUND((s.COSC_FECHASINCRONIZACION - c.COCP_FECHACARGA) * 24, 2) as TiempoSincronizacionHoras,
                        ROW_NUMBER() OVER (ORDER BY s.COSC_FECHASINCRONIZACION DESC) AS RNUM
                    FROM {TABLA} s
                    INNER JOIN CO_EVENTOSCARGAPROCESO c ON s.COSC_COCP_EVENTOCARGAPROCESOID = c.COCP_EVENTOCARGAPROCESOID
                    {whereClause}
                ) WHERE RNUM > :offset AND RNUM <= :limit";

            var resultados = await connection.QueryAsync<SincCargaProcesoDealerMap>(sql, parameters);
            var lista = resultados.Select(r => new SincCargaProcesoDealer
            {
                SincCargaProcesoDealerId = r.SincCargaProcesoDealerId,
                Proceso = r.Proceso,
                EventoCargaProcesoId = r.EventoCargaProcesoId,
                DmsOrigen = r.DmsOrigen,
                DealerBac = r.DealerBac,
                NombreDealer = r.NombreDealer,
                FechaSincronizacion = r.FechaSincronizacion,
                RegistrosSincronizados = r.RegistrosSincronizados,
                TokenConfirmacion = r.TokenConfirmacion ?? string.Empty,
                FechaAlta = r.FechaAlta,
                UsuarioAlta = r.UsuarioAlta,
                FechaModificacion = r.FechaModificacion,
                UsuarioModificacion = r.UsuarioModificacion
            }).ToList();

            _logger.LogInformation("✅ [REPOSITORY] Se obtuvieron {Cantidad} registros de sincronización de {Total} totales (Página {Page})", 
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
    public async Task<bool> ExisteRegistroAsync(string proceso, int eventoCargaProcesoId, string dealerBac)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM CO_SINCRONIZACIONCARGAPROCESODEALER s
            INNER JOIN CO_EVENTOSCARGAPROCESO c ON s.COSC_COCP_EVENTOCARGAPROCESOID = c.COCP_EVENTOCARGAPROCESOID
            WHERE c.COCP_PROCESO = :Proceso 
            AND s.COSC_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId
            AND s.COSC_DEALERBAC = :DealerBac";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Verificando existencia de registro - Proceso: {Proceso}, EventoCargaProcesoId: {EventoCargaProcesoId}, DealerBac: {DealerBac}",
                proceso, eventoCargaProcesoId, dealerBac);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var count = await connection.ExecuteScalarAsync<int>(sql, new 
            { 
                Proceso = proceso, 
                EventoCargaProcesoId = eventoCargaProcesoId, 
                DealerBac = dealerBac 
            });

            var existe = count > 0;
            _logger.LogInformation(
                "✅ [REPOSITORY] Registro (Proceso: '{Proceso}', EventoCargaProcesoId: {EventoCargaProcesoId}, DealerBac: '{DealerBac}') existe: {Existe}",
                proceso, eventoCargaProcesoId, dealerBac, existe);

            return existe;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al verificar existencia. ErrorCode: {ErrorCode}",
                ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<SincCargaProcesoDealer?> ObtenerPorProcesoCargaYDealerAsync(string proceso, int eventoCargaProcesoId, string dealerBac)
    {
        const string sql = @"
            SELECT
                s.COSC_SINCARGAPROCESODEALERID as SincCargaProcesoDealerId,
                c.COCP_PROCESO as Proceso,
                s.COSC_COCP_EVENTOCARGAPROCESOID as EventoCargaProcesoId,
                s.COSC_DMSORIGEN as DmsOrigen,
                s.COSC_DEALERBAC as DealerBac,
                s.COSC_NOMBREDEALER as NombreDealer,
                s.COSC_FECHASINCRONIZACION as FechaSincronizacion,
                s.COSC_REGISTROSSINCRONIZADOS as RegistrosSincronizados,
                s.COSC_TOKENCONFIRMACION as TokenConfirmacion,
                s.COSC_FECHAALTA as FechaAlta,
                s.COSC_USUARIOALTA as UsuarioAlta,
                s.COSC_FECHAMODIFICACION as FechaModificacion,
                s.COSC_USUARIOMODIFICACION as UsuarioModificacion
            FROM CO_SINCRONIZACIONCARGAPROCESODEALER s
            INNER JOIN CO_EVENTOSCARGAPROCESO c ON s.COSC_COCP_EVENTOCARGAPROCESOID = c.COCP_EVENTOCARGAPROCESOID
            WHERE c.COCP_PROCESO = :Proceso
            AND s.COSC_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId
            AND s.COSC_DEALERBAC = :DealerBac
            AND ROWNUM = 1";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Obteniendo registro por Proceso, EventoCargaProcesoId y DealerBac - Proceso: {Proceso}, EventoCargaProcesoId: {EventoCargaProcesoId}, DealerBac: {DealerBac}",
                proceso, eventoCargaProcesoId, dealerBac);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<SincCargaProcesoDealer>(sql, new
            {
                Proceso = proceso,
                EventoCargaProcesoId = eventoCargaProcesoId,
                DealerBac = dealerBac
            });

            if (resultado == null)
            {
                _logger.LogInformation("ℹ️ [REPOSITORY] No se encontró registro para esta combinación");
            }
            else
            {
                _logger.LogInformation("✅ [REPOSITORY] Registro encontrado. ID: {Id}, FechaSincronizacion: {Fecha}",
                    resultado.SincCargaProcesoDealerId, resultado.FechaSincronizacion);
            }

            return resultado;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle en {Method}. ErrorCode: {ErrorCode}",
                nameof(ObtenerPorProcesoCargaYDealerAsync), ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExisteEventoCargaProcesoIdAsync(int eventoCargaProcesoId)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM CO_EVENTOSCARGAPROCESO 
            WHERE COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId
            AND COCP_ACTUAL = 1";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Verificando existencia de EventoCargaProcesoId: {EventoCargaProcesoId}",
                eventoCargaProcesoId);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var count = await connection.ExecuteScalarAsync<int>(sql, new 
            { 
                EventoCargaProcesoId = eventoCargaProcesoId
            });

            var existe = count > 0;
            _logger.LogInformation(
                "✅ [REPOSITORY] EventoCargaProcesoId {EventoCargaProcesoId} existe: {Existe}",
                eventoCargaProcesoId, existe);

            return existe;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al verificar EventoCargaProcesoId. ErrorCode: {ErrorCode}",
                ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<SincCargaProcesoDealer> CrearAsync(SincCargaProcesoDealer entidad, string usuarioAlta)
    {
        var sqlInsert = $@"
            INSERT INTO CO_SINCRONIZACIONCARGAPROCESODEALER (
                COSC_SINCARGAPROCESODEALERID,
                COSC_COCP_EVENTOCARGAPROCESOID,
                COSC_DMSORIGEN,
                COSC_DEALERBAC,
                COSC_NOMBREDEALER,
                COSC_FECHASINCRONIZACION,
                COSC_REGISTROSSINCRONIZADOS,
                COSC_TOKENCONFIRMACION,
                COSC_FECHAALTA,
                COSC_USUARIOALTA
            ) VALUES (
                {SECUENCIA}.NEXTVAL,
                :EventoCargaProcesoId,
                :DmsOrigen,
                :DealerBac,
                :NombreDealer,
                :FechaSincronizacion,
                :RegistrosSincronizados,
                :TokenConfirmacion,
                SYSDATE,
                :UsuarioAlta
            ) RETURNING COSC_SINCARGAPROCESODEALERID INTO :Id";

        // SQL para obtener COCP_DEALERSTOTALES y COCP_REGISTROS a partir de EventoCargaProcesoId
        const string sqlObtenerCarga = @"
            SELECT 
                COCP_EVENTOCARGAPROCESOID as EventoCargaProcesoId,
                COCP_DEALERSTOTALES as DealersTotales,
                COCP_REGISTROS as Registros
            FROM CO_EVENTOSCARGAPROCESO
            WHERE COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId
            AND COCP_ACTUAL = 1";

        // SQL para contar dealers sincronizados
        const string sqlContarDealers = @"
            SELECT COUNT(*)
            FROM CO_SINCRONIZACIONCARGAPROCESODEALER
            WHERE COSC_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Iniciando creación de registro de sincronización con actualización automática de contadores. EventoCargaProcesoId: {EventoCargaProcesoId}, DealerBac: {DealerBac}, Usuario: {Usuario}",
                entidad.EventoCargaProcesoId, entidad.DealerBac, usuarioAlta);

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Validar que existe el EventoCargaProcesoId y obtener DealersTotales
                var cargaInfo = await connection.QueryFirstOrDefaultAsync<EventoCargaProcesoInfo>(
                    sqlObtenerCarga,
                    new { EventoCargaProcesoId = entidad.EventoCargaProcesoId },
                    transaction);

                if (cargaInfo == null)
                {
                    _logger.LogWarning(
                        "⚠️ [REPOSITORY] No se encontró registro de evento de carga con EventoCargaProcesoId: {EventoCargaProcesoId} y COCP_ACTUAL=1",
                        entidad.EventoCargaProcesoId);
                    throw new NotFoundException(
                        $"No se encontró un registro de evento de carga activo con EventoCargaProcesoId {entidad.EventoCargaProcesoId}",
                        "EventoCargaProceso",
                        entidad.EventoCargaProcesoId.ToString());
                }

                int eventoCargaProcesoId = cargaInfo.EventoCargaProcesoId;
                int dealersTotales = cargaInfo.DealersTotales;

                _logger.LogInformation(
                    "📊 [REPOSITORY] Evento de carga encontrado. COCP_EVENTOCARGAPROCESOID: {EventoCargaProcesoId}, DealersTotales: {DealersTotales}",
                    eventoCargaProcesoId, dealersTotales);

                // 2. Insertar registro de sincronización (sin COSC_PROCESO - se obtiene mediante JOIN)
                var parametersInsert = new DynamicParameters();
                parametersInsert.Add("EventoCargaProcesoId", entidad.EventoCargaProcesoId);
                parametersInsert.Add("DmsOrigen", entidad.DmsOrigen);
                parametersInsert.Add("DealerBac", entidad.DealerBac);
                parametersInsert.Add("NombreDealer", entidad.NombreDealer);
                parametersInsert.Add("FechaSincronizacion", entidad.FechaSincronizacion);
                parametersInsert.Add("RegistrosSincronizados", entidad.RegistrosSincronizados);
                parametersInsert.Add("TokenConfirmacion", entidad.TokenConfirmacion);
                parametersInsert.Add("UsuarioAlta", usuarioAlta);
                parametersInsert.Add("Id", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

                await connection.ExecuteAsync(sqlInsert, parametersInsert, transaction);

                var nuevoId = parametersInsert.Get<int>("Id");

                _logger.LogInformation(
                    "✅ [REPOSITORY] Registro de sincronización creado. ID: {Id}",
                    nuevoId);

                // 3. Contar dealers sincronizados (incluyendo el recién insertado)
                var dealersSincronizados = await connection.ExecuteScalarAsync<int>(
                    sqlContarDealers,
                    new { EventoCargaProcesoId = entidad.EventoCargaProcesoId },
                    transaction);

                // 4. Calcular porcentaje
                decimal porcDealersSinc = 0.00m;
                if (dealersTotales > 0)
                {
                    porcDealersSinc = Math.Round((decimal)dealersSincronizados / dealersTotales * 100, 2);
                }

                _logger.LogInformation(
                    "📊 [REPOSITORY] Contadores calculados. DealersSincronizados: {DealersSinc}, DealersTotales: {DealersTotales}, PorcDealersSinc: {Porc}%",
                    dealersSincronizados, dealersTotales, porcDealersSinc);

                // 5. Actualizar contadores en CO_EVENTOSCARGAPROCESO usando el repositorio correspondiente
                var filasActualizadas = await _eventoCargaProcesoRepository.ActualizarContadoresDealersAsync(
                    eventoCargaProcesoId,
                    dealersSincronizados,
                    porcDealersSinc,
                    usuarioAlta,
                    transaction);

                if (filasActualizadas == 0)
                {
                    _logger.LogWarning(
                        "⚠️ [REPOSITORY] No se actualizaron contadores de dealers. COCP_EVENTOCARGAPROCESOID: {EventoCargaProcesoId}",
                        eventoCargaProcesoId);
                }
                else
                {
                    _logger.LogInformation(
                        "✅ [REPOSITORY] Contadores actualizados en CO_EVENTOSCARGAPROCESO. COCP_EVENTOCARGAPROCESOID: {EventoCargaProcesoId}",
                        eventoCargaProcesoId);
                }

                // 6. Commit de la transacción
                transaction.Commit();

                _logger.LogInformation(
                    "✅ [REPOSITORY] Transacción completada exitosamente. Registro ID: {Id}, Contadores actualizados: DealersSincronizados={DealersSinc}, PorcDealersSinc={Porc}%",
                    nuevoId, dealersSincronizados, porcDealersSinc);

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
                "❌ [REPOSITORY] Error Oracle al crear registro. EventoCargaProcesoId: {EventoCargaProcesoId}, DealerBac: {DealerBac}, ErrorCode: {ErrorCode}",
                entidad.EventoCargaProcesoId, entidad.DealerBac, ex.Number);
            throw new DataAccessException("Error al crear el registro en la base de datos", ex);
        }
    }
}

