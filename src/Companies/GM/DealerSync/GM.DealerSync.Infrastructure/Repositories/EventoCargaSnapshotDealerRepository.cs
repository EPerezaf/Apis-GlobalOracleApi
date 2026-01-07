using Dapper;
using GM.DealerSync.Domain.Entities;
using GM.DealerSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.DealerSync.Infrastructure.Repositories;

/// <summary>
/// Repository para acceso a datos de EventoCargaSnapshotDealer usando Dapper.
/// Tabla: CO_EVENTOSCARGASNAPSHOTDEALERS
/// </summary>
public class EventoCargaSnapshotDealerRepository : IEventoCargaSnapshotDealerRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<EventoCargaSnapshotDealerRepository> _logger;
    private const string TABLA = "CO_EVENTOSCARGASNAPSHOTDEALERS";

    public EventoCargaSnapshotDealerRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<EventoCargaSnapshotDealerRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<EventoCargaSnapshotDealer>> GetDealersByUrlWebhookAsync(string urlWebhook, int eventoCargaProcesoId)
    {
        const string sql = @"
            SELECT 
                COSD_EVENTOCARGASNAPDEALERID as EventoCargaSnapshotDealerId,
                COSD_COCP_EVENTOCARGAPROCESOID as EventoCargaProcesoId,
                COSD_DEALERBAC as DealerBac,
                COSD_NOMBREDEALER as NombreDealer,
                COSD_DMS as Dms,
                COSD_URLWEBHOOK as UrlWebhook,
                COSD_SECRETKEY as SecretKey,
                COSD_ESTADOWEBHOOK as EstadoWebhook,
                COSD_INTENTOSWEBHOOK as IntentosWebhook,
                COSD_ULTIMOINTENTOWEBHOOK as UltimoIntentoWebhook,
                COSD_ULTIMOERRORWEBHOOK as UltimoErrorWebhook
            FROM CO_EVENTOSCARGASNAPSHOTDEALERS
            WHERE COSD_URLWEBHOOK = :UrlWebhook
              AND COSD_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Obteniendo dealers por UrlWebhook: {UrlWebhook}, EventoCargaProcesoId: {EventoCargaProcesoId}",
                urlWebhook, eventoCargaProcesoId);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultados = await connection.QueryAsync<EventoCargaSnapshotDealer>(sql, new
            {
                UrlWebhook = urlWebhook,
                EventoCargaProcesoId = eventoCargaProcesoId
            });

            var lista = resultados.ToList();

            _logger.LogInformation(
                "✅ [REPOSITORY] {Cantidad} dealers obtenidos para UrlWebhook: {UrlWebhook}",
                lista.Count, urlWebhook);

            return lista;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex,
                "❌ [REPOSITORY] Error Oracle en {Method}. ErrorCode: {ErrorCode}",
                nameof(GetDealersByUrlWebhookAsync), ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task UpdateWebhookStatusToExitosoAsync(string urlWebhook, int eventoCargaProcesoId, string ackToken, string currentUser)
    {
        const string sql = @"
            UPDATE CO_EVENTOSCARGASNAPSHOTDEALERS
            SET COSD_FECHAMODIFICACION = SYSDATE,
                COSD_USUARIOMODIFICACION = :UsuarioModificacion,
                COSD_INTENTOSWEBHOOK = NVL(COSD_INTENTOSWEBHOOK, 0) + 1,
                COSD_ULTIMOINTENTOWEBHOOK = SYSDATE,
                COSD_ULTIMOERRORWEBHOOK = NULL,
                COSD_ESTADOWEBHOOK = 'EXITOSO'
            WHERE COSD_URLWEBHOOK = :UrlWebhook
              AND COSD_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Actualizando estado webhook a EXITOSO - UrlWebhook: {UrlWebhook}, EventoCargaProcesoId: {EventoCargaProcesoId}, Usuario: {User}",
                urlWebhook, eventoCargaProcesoId, currentUser);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                UrlWebhook = urlWebhook,
                EventoCargaProcesoId = eventoCargaProcesoId,
                UsuarioModificacion = currentUser
            });

            _logger.LogInformation(
                "✅ [REPOSITORY] {RowsAffected} registros actualizados a EXITOSO para UrlWebhook: {UrlWebhook}",
                rowsAffected, urlWebhook);
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex,
                "❌ [REPOSITORY] Error Oracle al actualizar estado a EXITOSO. ErrorCode: {ErrorCode}",
                ex.Number);
            throw new DataAccessException("Error al actualizar estado en la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task UpdateWebhookStatusToFallidoAsync(string urlWebhook, int eventoCargaProcesoId, string errorMessage, string currentUser)
    {
        // Limitar el mensaje de error a 1000 caracteres (tamaño de la columna)
        var errorMsgTruncated = errorMessage.Length > 1000 
            ? errorMessage.Substring(0, 1000) 
            : errorMessage;

        const string sql = @"
            UPDATE CO_EVENTOSCARGASNAPSHOTDEALERS
            SET COSD_FECHAMODIFICACION = SYSDATE,
                COSD_USUARIOMODIFICACION = :UsuarioModificacion,
                COSD_INTENTOSWEBHOOK = NVL(COSD_INTENTOSWEBHOOK, 0) + 1,
                COSD_ULTIMOINTENTOWEBHOOK = SYSDATE,
                COSD_ULTIMOERRORWEBHOOK = :UltimoErrorWebhook,
                COSD_ESTADOWEBHOOK = 'FALLIDO'
            WHERE COSD_URLWEBHOOK = :UrlWebhook
              AND COSD_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Actualizando estado webhook a FALLIDO - UrlWebhook: {UrlWebhook}, EventoCargaProcesoId: {EventoCargaProcesoId}, Usuario: {User}",
                urlWebhook, eventoCargaProcesoId, currentUser);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                UrlWebhook = urlWebhook,
                EventoCargaProcesoId = eventoCargaProcesoId,
                UltimoErrorWebhook = errorMsgTruncated,
                UsuarioModificacion = currentUser
            });

            _logger.LogInformation(
                "✅ [REPOSITORY] {RowsAffected} registros actualizados a FALLIDO para UrlWebhook: {UrlWebhook}",
                rowsAffected, urlWebhook);
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex,
                "❌ [REPOSITORY] Error Oracle al actualizar estado a FALLIDO. ErrorCode: {ErrorCode}",
                ex.Number);
            throw new DataAccessException("Error al actualizar estado en la base de datos", ex);
        }
    }
}

