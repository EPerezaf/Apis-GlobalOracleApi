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
/// Repository para acceso a datos de SyncControl usando Dapper.
/// Tabla: CO_EVENTOSCARGASINCCONTROL
/// </summary>
public class SyncControlRepository : ISyncControlRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<SyncControlRepository> _logger;
    private const string TABLA = "CO_EVENTOSCARGASINCCONTROL";

    public SyncControlRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<SyncControlRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SyncControl?> GetByIdAsync(int syncControlId)
    {
        const string sql = @"
            SELECT 
                COES_SINCCONTROLID as SyncControlId,
                COES_PROCESSTYPE as ProcessType,
                COES_IDCARGA as IdCarga,
                COES_FECHACARGA as FechaCarga,
                COES_COCP_EVENTPROCESOID as EventoCargaProcesoId,
                COES_HANGFIREJOBID as HangfireJobId,
                COES_STATUS as Status,
                COES_FECHAINICIO as FechaInicio,
                COES_FECHAFIN as FechaFin,
                COES_WEBHOOKSTOTALES as WebhooksTotales,
                COES_WEBHOOKSPROCESADOS as WebhooksProcesados,
                COES_WEBHOOKSFALLIDOS as WebhooksFallidos,
                COES_WEBHOOKSOMITIDOS as WebhooksOmitidos,
                COES_ERRORMESSAGE as ErrorMessage,
                COES_ERRORDETAILS as ErrorDetails,
                FECHAREGISTRO as FechaRegistro,
                USUARIOREGISTRO as UsuarioRegistro,
                FECHAMODIFICACION as FechaModificacion,
                USUARIOMODIFICACION as UsuarioModificacion
            FROM CO_EVENTOSCARGASINCCONTROL
            WHERE COES_SINCCONTROLID = :SyncControlId";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo SyncControl por ID: {SyncControlId}", syncControlId);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<SyncControl>(sql, new { SyncControlId = syncControlId });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] SyncControl {SyncControlId} no encontrado", syncControlId);
                return null;
            }

            _logger.LogInformation("✅ [REPOSITORY] SyncControl {SyncControlId} obtenido exitosamente", syncControlId);
            return resultado;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle en {Method}. ErrorCode: {ErrorCode}",
                nameof(GetByIdAsync), ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<SyncControl?> GetByProcessAsync(string processType, string idCarga, DateTime fechaCarga)
    {
        const string sql = @"
            SELECT 
                COES_SINCCONTROLID as SyncControlId,
                COES_PROCESSTYPE as ProcessType,
                COES_IDCARGA as IdCarga,
                COES_FECHACARGA as FechaCarga,
                COES_COCP_EVENTPROCESOID as EventoCargaProcesoId,
                COES_HANGFIREJOBID as HangfireJobId,
                COES_STATUS as Status,
                COES_FECHAINICIO as FechaInicio,
                COES_FECHAFIN as FechaFin,
                COES_WEBHOOKSTOTALES as WebhooksTotales,
                COES_WEBHOOKSPROCESADOS as WebhooksProcesados,
                COES_WEBHOOKSFALLIDOS as WebhooksFallidos,
                COES_WEBHOOKSOMITIDOS as WebhooksOmitidos,
                COES_ERRORMESSAGE as ErrorMessage,
                COES_ERRORDETAILS as ErrorDetails,
                FECHAREGISTRO as FechaRegistro,
                USUARIOREGISTRO as UsuarioRegistro,
                FECHAMODIFICACION as FechaModificacion,
                USUARIOMODIFICACION as UsuarioModificacion
            FROM CO_EVENTOSCARGASINCCONTROL
            WHERE COES_PROCESSTYPE = :ProcessType
              AND COES_IDCARGA = :IdCarga
              AND TRUNC(COES_FECHACARGA) = TRUNC(:FechaCarga)";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo SyncControl por ProcessType: {ProcessType}, IdCarga: {IdCarga}, FechaCarga: {FechaCarga}",
                processType, idCarga, fechaCarga);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<SyncControl>(sql, new
            {
                ProcessType = processType,
                IdCarga = idCarga,
                FechaCarga = fechaCarga
            });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] SyncControl no encontrado para ProcessType: {ProcessType}, IdCarga: {IdCarga}",
                    processType, idCarga);
            }
            else
            {
                _logger.LogInformation("✅ [REPOSITORY] SyncControl encontrado: {SyncControlId}", resultado.SyncControlId);
            }

            return resultado;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle en {Method}. ErrorCode: {ErrorCode}",
                nameof(GetByProcessAsync), ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<List<SyncControl>> GetPendingProcessesAsync()
    {
        const string sql = @"
            SELECT 
                COES_SINCCONTROLID as SyncControlId,
                COES_PROCESSTYPE as ProcessType,
                COES_IDCARGA as IdCarga,
                COES_FECHACARGA as FechaCarga,
                COES_COCP_EVENTPROCESOID as EventoCargaProcesoId,
                COES_HANGFIREJOBID as HangfireJobId,
                COES_STATUS as Status,
                COES_FECHAINICIO as FechaInicio,
                COES_FECHAFIN as FechaFin,
                COES_WEBHOOKSTOTALES as WebhooksTotales,
                COES_WEBHOOKSPROCESADOS as WebhooksProcesados,
                COES_WEBHOOKSFALLIDOS as WebhooksFallidos,
                COES_WEBHOOKSOMITIDOS as WebhooksOmitidos,
                COES_ERRORMESSAGE as ErrorMessage,
                COES_ERRORDETAILS as ErrorDetails,
                FECHAREGISTRO as FechaRegistro,
                USUARIOREGISTRO as UsuarioRegistro,
                FECHAMODIFICACION as FechaModificacion,
                USUARIOMODIFICACION as UsuarioModificacion
            FROM CO_EVENTOSCARGASINCCONTROL
            WHERE COES_STATUS = 'PENDING'
            ORDER BY FECHAREGISTRO ASC";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo procesos pendientes");

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultados = await connection.QueryAsync<SyncControl>(sql);
            var lista = resultados.ToList();

            _logger.LogInformation("✅ [REPOSITORY] {Cantidad} procesos pendientes obtenidos", lista.Count);
            return lista;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle en {Method}. ErrorCode: {ErrorCode}",
                nameof(GetPendingProcessesAsync), ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<SyncControl?> GetByHangfireJobIdAsync(string hangfireJobId)
    {
        const string sql = @"
            SELECT 
                COES_SINCCONTROLID as SyncControlId,
                COES_PROCESSTYPE as ProcessType,
                COES_IDCARGA as IdCarga,
                COES_FECHACARGA as FechaCarga,
                COES_COCP_EVENTPROCESOID as EventoCargaProcesoId,
                COES_HANGFIREJOBID as HangfireJobId,
                COES_STATUS as Status,
                COES_FECHAINICIO as FechaInicio,
                COES_FECHAFIN as FechaFin,
                COES_WEBHOOKSTOTALES as WebhooksTotales,
                COES_WEBHOOKSPROCESADOS as WebhooksProcesados,
                COES_WEBHOOKSFALLIDOS as WebhooksFallidos,
                COES_WEBHOOKSOMITIDOS as WebhooksOmitidos,
                COES_ERRORMESSAGE as ErrorMessage,
                COES_ERRORDETAILS as ErrorDetails,
                FECHAREGISTRO as FechaRegistro,
                USUARIOREGISTRO as UsuarioRegistro,
                FECHAMODIFICACION as FechaModificacion,
                USUARIOMODIFICACION as UsuarioModificacion
            FROM CO_EVENTOSCARGASINCCONTROL
            WHERE COES_HANGFIREJOBID = :HangfireJobId";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo SyncControl por HangfireJobId: {HangfireJobId}", hangfireJobId);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<SyncControl>(sql, new { HangfireJobId = hangfireJobId });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] SyncControl no encontrado para HangfireJobId: {HangfireJobId}", hangfireJobId);
            }

            return resultado;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle en {Method}. ErrorCode: {ErrorCode}",
                nameof(GetByHangfireJobIdAsync), ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<SyncControl> CreateAsync(SyncControl syncControl, string currentUser)
    {
        const string sql = @"
            INSERT INTO CO_EVENTOSCARGASINCCONTROL 
                (COES_PROCESSTYPE, COES_IDCARGA, COES_FECHACARGA, COES_COCP_EVENTPROCESOID,
                 COES_STATUS, COES_WEBHOOKSTOTALES, USUARIOREGISTRO)
            VALUES 
                (:ProcessType, :IdCarga, :FechaCarga, :EventoCargaProcesoId,
                 :Status, :WebhooksTotales, :UsuarioRegistro)
            RETURNING COES_SINCCONTROLID INTO :SyncControlId";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Creando SyncControl - ProcessType: {ProcessType}, IdCarga: {IdCarga}, Usuario: {User}",
                syncControl.ProcessType, syncControl.IdCarga, currentUser);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            parameters.Add("ProcessType", syncControl.ProcessType);
            parameters.Add("IdCarga", syncControl.IdCarga);
            parameters.Add("FechaCarga", syncControl.FechaCarga);
            parameters.Add("EventoCargaProcesoId", syncControl.EventoCargaProcesoId);
            parameters.Add("Status", syncControl.Status);
            parameters.Add("WebhooksTotales", syncControl.WebhooksTotales);
            parameters.Add("UsuarioRegistro", currentUser);
            parameters.Add("SyncControlId", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

            await connection.ExecuteAsync(sql, parameters);

            syncControl.SyncControlId = parameters.Get<int>("SyncControlId");
            syncControl.UsuarioRegistro = currentUser;
            syncControl.FechaRegistro = DateTimeHelper.GetMexicoDateTime();

            _logger.LogInformation("✅ [REPOSITORY] SyncControl {SyncControlId} creado exitosamente", syncControl.SyncControlId);
            return syncControl;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al crear SyncControl. ErrorCode: {ErrorCode}",
                ex.Number);
            throw new DataAccessException("Error al crear SyncControl en la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<SyncControl> UpdateAsync(SyncControl syncControl, string currentUser)
    {
        const string sql = @"
            UPDATE CO_EVENTOSCARGASINCCONTROL
            SET COES_PROCESSTYPE = :ProcessType,
                COES_IDCARGA = :IdCarga,
                COES_FECHACARGA = :FechaCarga,
                COES_COCP_EVENTPROCESOID = :EventoCargaProcesoId,
                COES_HANGFIREJOBID = :HangfireJobId,
                COES_STATUS = :Status,
                COES_FECHAINICIO = :FechaInicio,
                COES_FECHAFIN = :FechaFin,
                COES_WEBHOOKSTOTALES = :WebhooksTotales,
                COES_WEBHOOKSPROCESADOS = :WebhooksProcesados,
                COES_WEBHOOKSFALLIDOS = :WebhooksFallidos,
                COES_WEBHOOKSOMITIDOS = :WebhooksOmitidos,
                COES_ERRORMESSAGE = :ErrorMessage,
                COES_ERRORDETAILS = :ErrorDetails,
                FECHAMODIFICACION = SYSDATE,
                USUARIOMODIFICACION = :UsuarioModificacion
            WHERE COES_SINCCONTROLID = :SyncControlId";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Actualizando SyncControl {SyncControlId} - Status: {Status}, Usuario: {User}",
                syncControl.SyncControlId, syncControl.Status, currentUser);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            parameters.Add("SyncControlId", syncControl.SyncControlId);
            parameters.Add("ProcessType", syncControl.ProcessType);
            parameters.Add("IdCarga", syncControl.IdCarga);
            parameters.Add("FechaCarga", syncControl.FechaCarga);
            parameters.Add("EventoCargaProcesoId", syncControl.EventoCargaProcesoId);
            parameters.Add("HangfireJobId", syncControl.HangfireJobId);
            parameters.Add("Status", syncControl.Status);
            parameters.Add("FechaInicio", syncControl.FechaInicio);
            parameters.Add("FechaFin", syncControl.FechaFin);
            parameters.Add("WebhooksTotales", syncControl.WebhooksTotales);
            parameters.Add("WebhooksProcesados", syncControl.WebhooksProcesados);
            parameters.Add("WebhooksFallidos", syncControl.WebhooksFallidos);
            parameters.Add("WebhooksOmitidos", syncControl.WebhooksOmitidos);
            parameters.Add("ErrorMessage", syncControl.ErrorMessage);
            parameters.Add("ErrorDetails", syncControl.ErrorDetails);
            parameters.Add("UsuarioModificacion", currentUser);

            var rowsAffected = await connection.ExecuteAsync(sql, parameters);

            if (rowsAffected == 0)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] No se actualizó ningún registro para SyncControlId: {SyncControlId}", syncControl.SyncControlId);
                throw new NotFoundException($"SyncControl con ID {syncControl.SyncControlId} no encontrado", "SyncControl", syncControl.SyncControlId.ToString());
            }

            syncControl.FechaModificacion = DateTimeHelper.GetMexicoDateTime();
            syncControl.UsuarioModificacion = currentUser;

            _logger.LogInformation("✅ [REPOSITORY] SyncControl {SyncControlId} actualizado exitosamente", syncControl.SyncControlId);
            return syncControl;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al actualizar SyncControl {SyncControlId}. ErrorCode: {ErrorCode}",
                syncControl.SyncControlId, ex.Number);
            throw new DataAccessException("Error al actualizar SyncControl en la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task UpdateStatusToRunningAsync(int syncControlId, string hangfireJobId, string currentUser)
    {
        const string sql = @"
            UPDATE CO_EVENTOSCARGASINCCONTROL
            SET COES_STATUS = 'RUNNING',
                COES_HANGFIREJOBID = :HangfireJobId,
                COES_FECHAINICIO = SYSDATE,
                FECHAMODIFICACION = SYSDATE,
                USUARIOMODIFICACION = :UsuarioModificacion
            WHERE COES_SINCCONTROLID = :SyncControlId";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Actualizando status a RUNNING - SyncControlId: {SyncControlId}, HangfireJobId: {HangfireJobId}, Usuario: {User}",
                syncControlId, hangfireJobId, currentUser);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                SyncControlId = syncControlId,
                HangfireJobId = hangfireJobId,
                UsuarioModificacion = currentUser
            });

            if (rowsAffected == 0)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] No se actualizó ningún registro para SyncControlId: {SyncControlId}", syncControlId);
                throw new NotFoundException($"SyncControl con ID {syncControlId} no encontrado", "SyncControl", syncControlId.ToString());
            }

            _logger.LogInformation("✅ [REPOSITORY] Status actualizado a RUNNING para SyncControlId: {SyncControlId}", syncControlId);
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al actualizar status a RUNNING. ErrorCode: {ErrorCode}",
                ex.Number);
            throw new DataAccessException("Error al actualizar status en la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task UpdateStatusToCompletedAsync(int syncControlId, int webhooksProcesados, int webhooksFallidos, int webhooksOmitidos, string currentUser)
    {
        const string sql = @"
            UPDATE CO_EVENTOSCARGASINCCONTROL
            SET COES_STATUS = 'COMPLETED',
                COES_FECHAFIN = SYSDATE,
                COES_WEBHOOKSPROCESADOS = :WebhooksProcesados,
                COES_WEBHOOKSFALLIDOS = :WebhooksFallidos,
                COES_WEBHOOKSOMITIDOS = :WebhooksOmitidos,
                FECHAMODIFICACION = SYSDATE,
                USUARIOMODIFICACION = :UsuarioModificacion
            WHERE COES_SINCCONTROLID = :SyncControlId";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Actualizando status a COMPLETED - SyncControlId: {SyncControlId}, WebhooksProcesados: {WebhooksProcesados}, WebhooksFallidos: {WebhooksFallidos}, WebhooksOmitidos: {WebhooksOmitidos}, Usuario: {User}",
                syncControlId, webhooksProcesados, webhooksFallidos, webhooksOmitidos, currentUser);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                SyncControlId = syncControlId,
                WebhooksProcesados = webhooksProcesados,
                WebhooksFallidos = webhooksFallidos,
                WebhooksOmitidos = webhooksOmitidos,
                UsuarioModificacion = currentUser
            });

            if (rowsAffected == 0)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] No se actualizó ningún registro para SyncControlId: {SyncControlId}", syncControlId);
                throw new NotFoundException($"SyncControl con ID {syncControlId} no encontrado", "SyncControl", syncControlId.ToString());
            }

            _logger.LogInformation("✅ [REPOSITORY] Status actualizado a COMPLETED para SyncControlId: {SyncControlId}", syncControlId);
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al actualizar status a COMPLETED. ErrorCode: {ErrorCode}",
                ex.Number);
            throw new DataAccessException("Error al actualizar status en la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task UpdateStatusToFailedAsync(int syncControlId, string errorMessage, string? errorDetails, string currentUser)
    {
        const string sql = @"
            UPDATE CO_EVENTOSCARGASINCCONTROL
            SET COES_STATUS = 'FAILED',
                COES_FECHAFIN = SYSDATE,
                COES_ERRORMESSAGE = :ErrorMessage,
                COES_ERRORDETAILS = :ErrorDetails,
                FECHAMODIFICACION = SYSDATE,
                USUARIOMODIFICACION = :UsuarioModificacion
            WHERE COES_SINCCONTROLID = :SyncControlId";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Actualizando status a FAILED - SyncControlId: {SyncControlId}, Error: {ErrorMessage}, Usuario: {User}",
                syncControlId, errorMessage, currentUser);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var rowsAffected = await connection.ExecuteAsync(sql, new
            {
                SyncControlId = syncControlId,
                ErrorMessage = errorMessage,
                ErrorDetails = errorDetails,
                UsuarioModificacion = currentUser
            });

            if (rowsAffected == 0)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] No se actualizó ningún registro para SyncControlId: {SyncControlId}", syncControlId);
                throw new NotFoundException($"SyncControl con ID {syncControlId} no encontrado", "SyncControl", syncControlId.ToString());
            }

            _logger.LogInformation("✅ [REPOSITORY] Status actualizado a FAILED para SyncControlId: {SyncControlId}", syncControlId);
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al actualizar status a FAILED. ErrorCode: {ErrorCode}",
                ex.Number);
            throw new DataAccessException("Error al actualizar status en la base de datos", ex);
        }
    }
}

