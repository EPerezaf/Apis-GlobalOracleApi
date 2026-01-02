using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.DealersSincronizacion.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;
using Shared.Security;

namespace GM.DealersSincronizacion.Infrastructure.Repositories;

/// <summary>
/// Repository para acceso a datos de Sincronización de Carga de Proceso por Dealer usando Dapper.
/// Tabla: CO_SINCRONIZACIONCARGAPROCESODEALER
/// </summary>
public class SincCargaProcesoDealerRepository : ISincCargaProcesoDealerRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<SincCargaProcesoDealerRepository> _logger;

    private const string TABLA = "CO_SINCRONIZACIONCARGAPROCESODEALER";
    private const string SECUENCIA = "SEQ_CO_SINCRONIZACIONCARGAPROCESODEALER";
    private const string TABLA_CARGA = "CO_EVENTOSCARGAPROCESO";

    public SincCargaProcesoDealerRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<SincCargaProcesoDealerRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SincCargaProcesoDealer?> ObtenerPorCargaYDealerAsync(int eventoCargaProcesoId, string dealerBac)
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
            WHERE s.COSC_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId
            AND s.COSC_DEALERBAC = :DealerBac
            AND ROWNUM = 1";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Verificando existencia de sincronización. EventoCargaProcesoId: {EventoCargaProcesoId}, DealerBac: {DealerBac}",
                eventoCargaProcesoId, dealerBac);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<SincCargaProcesoDealer>(sql, new
            {
                EventoCargaProcesoId = eventoCargaProcesoId,
                DealerBac = dealerBac
            });

            if (resultado == null)
            {
                _logger.LogInformation("ℹ️ [REPOSITORY] No existe registro previo para esta combinación");
            }
            else
            {
                _logger.LogInformation("✅ [REPOSITORY] Registro encontrado. ID: {Id}", resultado.SincCargaProcesoDealerId);
            }

            return resultado;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle en {Method}. ErrorCode: {ErrorCode}",
                nameof(ObtenerPorCargaYDealerAsync), ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<SincCargaProcesoDealer> CrearAsync(SincCargaProcesoDealer entidad, string usuarioAlta)
    {
        const string sqlInsert = @"
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
                :SincCargaProcesoDealerId,
                :EventoCargaProcesoId,
                :DmsOrigen,
                :DealerBac,
                :NombreDealer,
                :FechaSincronizacion,
                :RegistrosSincronizados,
                :TokenConfirmacion,
                :FechaAlta,
                :UsuarioAlta
            )";

        const string sqlObtenerCarga = @"
            SELECT 
                COCP_PROCESO as Proceso,
                COCP_REGISTROS as Registros,
                COCP_DEALERSTOTALES as DealersTotales,
                COCP_DEALERSSINCRONIZADOS as DealersSincronizados
            FROM CO_EVENTOSCARGAPROCESO
            WHERE COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";

        const string sqlContarDealers = @"
            SELECT COUNT(DISTINCT COSC_DEALERBAC)
            FROM CO_SINCRONIZACIONCARGAPROCESODEALER
            WHERE COSC_COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";

        const string sqlUpdateCarga = @"
            UPDATE CO_EVENTOSCARGAPROCESO
            SET 
                COCP_DEALERSSINCRONIZADOS = :DealersSincronizados,
                COCP_PORCDEALERSSINC = CASE 
                    WHEN COCP_DEALERSTOTALES > 0 
                    THEN ROUND((:DealersSincronizados * 100.0 / COCP_DEALERSTOTALES), 2)
                    ELSE 0.00 
                END,
                COCP_FECHAMODIFICACION = :FechaModificacion,
                COCP_USUARIOMODIFICACION = :UsuarioModificacion
            WHERE COCP_EVENTOCARGAPROCESOID = :EventoCargaProcesoId";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Creando registro de sincronización. DealerBac: {DealerBac}, EventoCargaProcesoId: {EventoCargaProcesoId}",
                entidad.DealerBac, entidad.EventoCargaProcesoId);

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Obtener siguiente ID de secuencia
                var idSql = $"SELECT {SECUENCIA}.NEXTVAL FROM DUAL";
                var nuevoId = await connection.ExecuteScalarAsync<int>(idSql, transaction: transaction);
                entidad.SincCargaProcesoDealerId = nuevoId;

                // Obtener datos de la carga
                var carga = await connection.QueryFirstOrDefaultAsync<dynamic>(sqlObtenerCarga, new
                {
                    EventoCargaProcesoId = entidad.EventoCargaProcesoId
                }, transaction: transaction);

                if (carga == null)
                {
                    throw new NotFoundException(
                        $"No se encontró el evento de carga de proceso con ID {entidad.EventoCargaProcesoId}",
                        "EventoCargaProceso",
                        entidad.EventoCargaProcesoId.ToString());
                }

                // ✅ Proceso y RegistrosSincronizados ya vienen de la carga en el Service
                // ✅ No se guarda COSC_PROCESO en la tabla (se obtiene mediante JOIN)
                // ✅ FechaSincronizacion y TokenConfirmacion ya se establecieron en el Service
                entidad.FechaAlta = DateTimeHelper.GetMexicoDateTime();
                entidad.UsuarioAlta = usuarioAlta;

                // Insertar registro (sin COSC_PROCESO)
                await connection.ExecuteAsync(sqlInsert, new
                {
                    SincCargaProcesoDealerId = entidad.SincCargaProcesoDealerId,
                    EventoCargaProcesoId = entidad.EventoCargaProcesoId,
                    DmsOrigen = entidad.DmsOrigen ?? "",
                    DealerBac = entidad.DealerBac,
                    NombreDealer = entidad.NombreDealer ?? "",
                    FechaSincronizacion = entidad.FechaSincronizacion,
                    RegistrosSincronizados = entidad.RegistrosSincronizados,
                    TokenConfirmacion = entidad.TokenConfirmacion,
                    FechaAlta = entidad.FechaAlta,
                    UsuarioAlta = entidad.UsuarioAlta
                }, transaction: transaction);

                // Contar dealers únicos sincronizados
                var dealersSincronizados = await connection.ExecuteScalarAsync<int>(sqlContarDealers, new
                {
                    EventoCargaProcesoId = entidad.EventoCargaProcesoId
                }, transaction: transaction);

                // Actualizar contadores en CO_EVENTOSCARGAPROCESO
                await connection.ExecuteAsync(sqlUpdateCarga, new
                {
                    DealersSincronizados = dealersSincronizados,
                    FechaModificacion = DateTimeHelper.GetMexicoDateTime(),
                    UsuarioModificacion = usuarioAlta,
                    EventoCargaProcesoId = entidad.EventoCargaProcesoId
                }, transaction: transaction);

                transaction.Commit();

                _logger.LogInformation("✅ [REPOSITORY] Registro creado exitosamente. ID: {Id}", entidad.SincCargaProcesoDealerId);
                return entidad;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al crear registro. ErrorCode: {ErrorCode}", ex.Number);
            throw new DataAccessException("Error al crear el registro en la base de datos", ex);
        }
    }
}

