using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.DealersSincronizacion.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.DealersSincronizacion.Infrastructure.Repositories;

/// <summary>
/// Repository para acceso a datos de Evento de Carga de Proceso usando Dapper (para dealers).
/// Tabla: CO_EVENTOSCARGAPROCESO
/// </summary>
public class EventoCargaProcesoRepository : IEventoCargaProcesoRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<EventoCargaProcesoRepository> _logger;

    private const string TABLA = "CO_EVENTOSCARGAPROCESO";

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

            var resultado = await connection.QueryFirstOrDefaultAsync<EventoCargaProceso>(sql, new { Id = id });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] Registro de evento de carga con ID {Id} no encontrado", id);
                return null;
            }

            _logger.LogInformation("✅ [REPOSITORY] Registro de evento de carga con ID {Id} obtenido exitosamente", id);
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

            var resultados = await connection.QueryAsync<EventoCargaProceso>(sql, parameters);
            var lista = resultados.ToList();

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
    public async Task<EventoCargaProceso?> ObtenerActualAsync()
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
            WHERE COCP_ACTUAL = 1
            AND ROWNUM = 1
            ORDER BY COCP_EVENTOCARGAPROCESOID DESC";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo registro actual de evento de carga de proceso");

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<EventoCargaProceso>(sql);

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] No se encontró registro actual de evento de carga de proceso");
                return null;
            }

            _logger.LogInformation("✅ [REPOSITORY] Registro actual obtenido exitosamente. ID: {Id}", resultado.EventoCargaProcesoId);
            return resultado;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle en {Method}. ErrorCode: {ErrorCode}",
                nameof(ObtenerActualAsync), ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<EventoCargaProceso?> ObtenerActualPorProcesoAsync(string proceso)
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
            WHERE COCP_ACTUAL = 1
            AND UPPER(COCP_PROCESO) = UPPER(:Proceso)
            AND ROWNUM = 1
            ORDER BY COCP_EVENTOCARGAPROCESOID DESC";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo registro actual de evento de carga de proceso para proceso: {Proceso}", proceso);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<EventoCargaProceso>(sql, new { Proceso = proceso });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] No se encontró registro actual de evento de carga de proceso para proceso: {Proceso}", proceso);
                return null;
            }

            _logger.LogInformation("✅ [REPOSITORY] Registro actual obtenido exitosamente para proceso {Proceso}. ID: {Id}", proceso, resultado.EventoCargaProcesoId);
            return resultado;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle en {Method} para proceso {Proceso}. ErrorCode: {ErrorCode}",
                nameof(ObtenerActualPorProcesoAsync), proceso, ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }
}

