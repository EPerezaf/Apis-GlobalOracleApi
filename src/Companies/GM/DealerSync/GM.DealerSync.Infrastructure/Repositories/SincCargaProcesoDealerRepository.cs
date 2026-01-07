using Dapper;
using GM.DealerSync.Domain.Entities;
using GM.DealerSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;
using Shared.Security;

namespace GM.DealerSync.Infrastructure.Repositories;

/// <summary>
/// Repository para acceso a datos de SincCargaProcesoDealer usando Dapper.
/// Tabla: CO_SINCRONIZACIONCARGAPROCESODEALER
/// </summary>
public class SincCargaProcesoDealerRepository : ISincCargaProcesoDealerRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<SincCargaProcesoDealerRepository> _logger;
    private const string TABLA = "CO_SINCRONIZACIONCARGAPROCESODEALER";
    private const string SECUENCIA = "SEQ_CO_SINCRONIZACIONCARGAPROCESODEALER";

    public SincCargaProcesoDealerRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<SincCargaProcesoDealerRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> GetCountByEventoCargaProcesoIdAsync(int eventoCargaProcesoId)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM CO_SINCRONIZACIONCARGAPROCESODEALER
            WHERE COSC_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Obteniendo conteo de registros sincronizados para EventoCargaProcesoId: {EventoCargaProcesoId}",
                eventoCargaProcesoId);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                EventoCargaProcesoId = eventoCargaProcesoId
            });

            _logger.LogInformation(
                "✅ [REPOSITORY] Conteo obtenido: {Count} registros para EventoCargaProcesoId: {EventoCargaProcesoId}",
                count, eventoCargaProcesoId);

            return count;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex,
                "❌ [REPOSITORY] Error Oracle en {Method}. ErrorCode: {ErrorCode}",
                nameof(GetCountByEventoCargaProcesoIdAsync), ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<int> GetTotalDealersCountAsync(int eventoCargaProcesoId)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM CO_EVENTOSCARGASNAPSHOTDEALERS
            WHERE COSD_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Obteniendo conteo total de dealers para EventoCargaProcesoId: {EventoCargaProcesoId}",
                eventoCargaProcesoId);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var count = await connection.ExecuteScalarAsync<int>(sql, new
            {
                EventoCargaProcesoId = eventoCargaProcesoId
            });

            _logger.LogInformation(
                "✅ [REPOSITORY] Conteo total de dealers: {Count} para EventoCargaProcesoId: {EventoCargaProcesoId}",
                count, eventoCargaProcesoId);

            return count;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex,
                "❌ [REPOSITORY] Error Oracle en {Method}. ErrorCode: {ErrorCode}",
                nameof(GetTotalDealersCountAsync), ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<SincCargaProcesoDealer> CreateAsync(SincCargaProcesoDealer entidad, string usuarioAlta)
    {
        const string sql = @"
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
                :Id,
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

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Creando registro de sincronización - EventoCargaProcesoId: {EventoCargaProcesoId}, DealerBac: {DealerBac}, Usuario: {User}",
                entidad.EventoCargaProcesoId, entidad.DealerBac, usuarioAlta);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            // Obtener el siguiente ID de la secuencia
            var nextIdSql = $"SELECT {SECUENCIA}.NEXTVAL FROM DUAL";
            var id = await connection.ExecuteScalarAsync<int>(nextIdSql);

            var parameters = new DynamicParameters();
            parameters.Add("Id", id);
            parameters.Add("EventoCargaProcesoId", entidad.EventoCargaProcesoId);
            parameters.Add("DmsOrigen", entidad.DmsOrigen);
            parameters.Add("DealerBac", entidad.DealerBac);
            parameters.Add("NombreDealer", entidad.NombreDealer);
            parameters.Add("FechaSincronizacion", entidad.FechaSincronizacion);
            parameters.Add("RegistrosSincronizados", entidad.RegistrosSincronizados);
            parameters.Add("TokenConfirmacion", entidad.TokenConfirmacion);
            parameters.Add("UsuarioAlta", usuarioAlta);

            await connection.ExecuteAsync(sql, parameters);

            entidad.SincCargaProcesoDealerId = id;
            entidad.UsuarioAlta = usuarioAlta;
            entidad.FechaAlta = DateTimeHelper.GetMexicoDateTime();

            _logger.LogInformation(
                "✅ [REPOSITORY] Registro de sincronización creado exitosamente. ID: {Id}",
                id);

            return entidad;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex,
                "❌ [REPOSITORY] Error Oracle al crear registro. ErrorCode: {ErrorCode}",
                ex.Number);
            throw new DataAccessException("Error al crear el registro en la base de datos", ex);
        }
    }
}

