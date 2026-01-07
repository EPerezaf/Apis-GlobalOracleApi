using Dapper;
using GM.DealerSync.Domain.Entities;
using GM.DealerSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.DealerSync.Infrastructure.Repositories;

/// <summary>
/// Repository para acceso a datos de Dealers usando Dapper.
/// Tablas: CO_EVENTOSCARGAPROCESO, CO_EVENTOSCARGASNAPSHOTDEALERS
/// </summary>
public class DealerRepository : IDealerRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<DealerRepository> _logger;

    public DealerRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<DealerRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int?> GetEventoCargaProcesoIdAsync(string processType, string idCarga)
    {
        var info = await GetEventoCargaProcesoInfoAsync(processType, idCarga);
        return info?.EventoCargaProcesoId;
    }

    /// <inheritdoc />
    public async Task<(int EventoCargaProcesoId, DateTime FechaCarga)?> GetEventoCargaProcesoInfoAsync(string processType, string idCarga)
    {
        const string sql = @"
            SELECT 
                COCP_EVENTOCARGAPROCESOID as EventoCargaProcesoId,
                COCP_FECHACARGA as FechaCarga
            FROM CO_EVENTOSCARGAPROCESO
            WHERE COCP_PROCESO = :ProcessType
              AND COCP_IDCARGA = :IdCarga
              AND ROWNUM = 1";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo EventoCargaProcesoId y FechaCarga - ProcessType: {ProcessType}, IdCarga: {IdCarga}",
                processType, idCarga);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<(int EventoCargaProcesoId, DateTime FechaCarga)?>(sql, new
            {
                ProcessType = processType,
                IdCarga = idCarga
            });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] EventoCargaProcesoId no encontrado para ProcessType: {ProcessType}, IdCarga: {IdCarga}",
                    processType, idCarga);
            }
            else
            {
                _logger.LogInformation("✅ [REPOSITORY] EventoCargaProcesoId encontrado: {EventoCargaProcesoId}, FechaCarga: {FechaCarga}",
                    resultado.Value.EventoCargaProcesoId, resultado.Value.FechaCarga);
            }

            return resultado;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle en {Method}. ErrorCode: {ErrorCode}",
                nameof(GetEventoCargaProcesoInfoAsync), ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<List<Dealer>> GetActiveDealersByProcessIdAsync(int eventoCargaProcesoId)
    {
        // Query que agrupa por URLWebhook y filtra solo dealers con EstadoWebhook != 'EXITOSO'
        // Agrupa todos los DealerBACs por URLWebhook usando LISTAGG
        const string sql = @"
            SELECT 
                COSD_URLWEBHOOK as UrlWebhook,
                MAX(COSD_SECRETKEY) as SecretKey,
                LISTAGG(COSD_DEALERBAC, ', ') WITHIN GROUP (ORDER BY COSD_DEALERBAC) as DealerBac,
                MAX(COSD_NOMBREDEALER) as NombreDealer,
                MAX(COSD_ESTADOWEBHOOK) as EstadoWebhook,
                :EventoCargaProcesoId as EventoCargaProcesoId
            FROM CO_EVENTOSCARGASNAPSHOTDEALERS
            WHERE COSD_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId
              AND (COSD_ESTADOWEBHOOK IS NULL OR COSD_ESTADOWEBHOOK != 'EXITOSO')
              AND COSD_URLWEBHOOK IS NOT NULL
            GROUP BY COSD_URLWEBHOOK
            ORDER BY COSD_URLWEBHOOK";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo dealers activos por EventoCargaProcesoId: {EventoCargaProcesoId}",
                eventoCargaProcesoId);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultados = await connection.QueryAsync<Dealer>(sql, new
            {
                EventoCargaProcesoId = eventoCargaProcesoId
            });

            var lista = resultados.ToList();

            _logger.LogInformation("✅ [REPOSITORY] {Cantidad} dealers activos obtenidos para EventoCargaProcesoId: {EventoCargaProcesoId}",
                lista.Count, eventoCargaProcesoId);

            return lista;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle en {Method}. ErrorCode: {ErrorCode}",
                nameof(GetActiveDealersByProcessIdAsync), ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task UpdateDealersSincronizadosAsync(int eventoCargaProcesoId, int dealersSincronizados, decimal porcentajeSincronizados, string currentUser)
    {
        const string sql = @"
            UPDATE CO_EVENTOSCARGAPROCESO
            SET COCP_DEALERSSINCRONIZADOS = :DealersSincronizados,
                COCP_PORCDEALERSSINC = :PorcentajeSincronizados,
                COCP_FECHAMODIFICACION = SYSDATE,
                COCP_USUARIOMODIFICACION = :UsuarioModificacion
            WHERE COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Actualizando dealers sincronizados en CO_EVENTOSCARGAPROCESO - EventoCargaProcesoId: {EventoCargaProcesoId}, DealersSincronizados: {DealersSincronizados}, Porcentaje: {Porcentaje}%",
                eventoCargaProcesoId, dealersSincronizados, porcentajeSincronizados);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                EventoCargaProcesoId = eventoCargaProcesoId,
                DealersSincronizados = dealersSincronizados,
                PorcentajeSincronizados = porcentajeSincronizados,
                UsuarioModificacion = currentUser
            });

            if (rowsAffected == 0)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] No se actualizó ningún registro para EventoCargaProcesoId: {EventoCargaProcesoId}", eventoCargaProcesoId);
                throw new NotFoundException($"EventoCargaProceso con ID {eventoCargaProcesoId} no encontrado", "EventoCargaProceso", eventoCargaProcesoId.ToString());
            }

            _logger.LogInformation("✅ [REPOSITORY] Dealers sincronizados actualizados exitosamente en CO_EVENTOSCARGAPROCESO para EventoCargaProcesoId: {EventoCargaProcesoId}", eventoCargaProcesoId);
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al actualizar dealers sincronizados. ErrorCode: {ErrorCode}",
                ex.Number);
            throw new DataAccessException("Error al actualizar dealers sincronizados en la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetEventoCargaProcesoEstatusAsync(string processType, string idCarga)
    {
        const string sql = @"
            SELECT COCP_ESTATUS
            FROM CO_EVENTOSCARGAPROCESO
            WHERE COCP_PROCESO = :ProcessType
              AND COCP_IDCARGA = :IdCarga
              AND ROWNUM = 1";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo Estatus de CO_EVENTOSCARGAPROCESO - ProcessType: {ProcessType}, IdCarga: {IdCarga}",
                processType, idCarga);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var estatus = await connection.QueryFirstOrDefaultAsync<string?>(sql, new
            {
                ProcessType = processType,
                IdCarga = idCarga
            });

            if (string.IsNullOrWhiteSpace(estatus))
            {
                _logger.LogWarning("⚠️ [REPOSITORY] Estatus no encontrado o nulo para ProcessType: {ProcessType}, IdCarga: {IdCarga}",
                    processType, idCarga);
            }
            else
            {
                _logger.LogInformation("✅ [REPOSITORY] Estatus obtenido: {Estatus} para ProcessType: {ProcessType}, IdCarga: {IdCarga}",
                    estatus, processType, idCarga);
            }

            return estatus;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle en {Method}. ErrorCode: {ErrorCode}",
                nameof(GetEventoCargaProcesoEstatusAsync), ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }
}

