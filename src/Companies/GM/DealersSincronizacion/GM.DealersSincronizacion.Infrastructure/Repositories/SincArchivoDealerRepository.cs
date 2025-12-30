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
/// Repository para acceso a datos de Sincronización de Archivos por Dealer usando Dapper.
/// </summary>
public class SincArchivoDealerRepository : ISincArchivoDealerRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<SincArchivoDealerRepository> _logger;

    private const string TABLA = "CO_SINCRONIZACIONARCHIVOSDEALERS";
    private const string SECUENCIA = "SEQ_COSA_SINCARCHIVODEALERID";
    private const string TABLA_CARGA = "CO_CARGAARCHIVOSINCRONIZACION";

    public SincArchivoDealerRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<SincArchivoDealerRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SincArchivoDealer?> ObtenerPorCargaYDealerAsync(int cargaArchivoSincronizacionId, string dealerBac)
    {
        const string sql = @"
            SELECT 
                s.COSA_SINCARCHIVODEALERID as SincArchivoDealerId,
                c.COCA_PROCESO as Proceso,
                s.COSA_COCA_CARGAARCHIVOSINID as CargaArchivoSincronizacionId,
                s.COSA_DMSORIGEN as DmsOrigen,
                s.COSA_DEALERBAC as DealerBac,
                s.COSA_NOMBREDEALER as NombreDealer,
                s.COSA_FECHASINCRONIZACION as FechaSincronizacion,
                s.COSA_REGISTROSSINCRONIZADOS as RegistrosSincronizados,
                s.COSA_TOKENCONFIRMACION as TokenConfirmacion,
                s.FECHAALTA as FechaAlta,
                s.USUARIOALTA as UsuarioAlta,
                s.FECHAMODIFICACION as FechaModificacion,
                s.USUARIOMODIFICACION as UsuarioModificacion
            FROM CO_SINCRONIZACIONARCHIVOSDEALERS s
            INNER JOIN CO_CARGAARCHIVOSINCRONIZACION c ON s.COSA_COCA_CARGAARCHIVOSINID = c.COCA_CARGAARCHIVOSINID
            WHERE s.COSA_COCA_CARGAARCHIVOSINID = :CargaArchivoSincronizacionId
            AND s.COSA_DEALERBAC = :DealerBac
            AND ROWNUM = 1";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Verificando existencia de sincronización. CargaId: {CargaId}, DealerBac: {DealerBac}",
                cargaArchivoSincronizacionId, dealerBac);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<SincArchivoDealer>(sql, new
            {
                CargaArchivoSincronizacionId = cargaArchivoSincronizacionId,
                DealerBac = dealerBac
            });

            if (resultado == null)
            {
                _logger.LogInformation("ℹ️ [REPOSITORY] No existe registro previo para esta combinación");
            }
            else
            {
                _logger.LogInformation("✅ [REPOSITORY] Registro encontrado. ID: {Id}", resultado.SincArchivoDealerId);
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
    public async Task<SincArchivoDealer> CrearAsync(SincArchivoDealer entidad, string usuarioAlta)
    {
        const string sqlInsert = @"
            INSERT INTO CO_SINCRONIZACIONARCHIVOSDEALERS (
                COSA_SINCARCHIVODEALERID,
                COSA_COCA_CARGAARCHIVOSINID,
                COSA_DMSORIGEN,
                COSA_DEALERBAC,
                COSA_NOMBREDEALER,
                COSA_FECHASINCRONIZACION,
                COSA_REGISTROSSINCRONIZADOS,
                COSA_TOKENCONFIRMACION,
                FECHAALTA,
                USUARIOALTA
            ) VALUES (
                :SincArchivoDealerId,
                :CargaArchivoSincronizacionId,
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
                COCA_PROCESO as Proceso,
                COCA_REGISTROS as Registros,
                COCA_DEALERSTOTALES as DealersTotales,
                COCA_DEALERSSONCRONIZADOS as DealersSincronizados
            FROM CO_CARGAARCHIVOSINCRONIZACION
            WHERE COCA_CARGAARCHIVOSINID = :CargaArchivoSincronizacionId";

        const string sqlContarDealers = @"
            SELECT COUNT(DISTINCT COSA_DEALERBAC)
            FROM CO_SINCRONIZACIONARCHIVOSDEALERS
            WHERE COSA_COCA_CARGAARCHIVOSINID = :CargaArchivoSincronizacionId";

        const string sqlUpdateCarga = @"
            UPDATE CO_CARGAARCHIVOSINCRONIZACION
            SET 
                COCA_DEALERSSONCRONIZADOS = :DealersSincronizados,
                COCA_PORCDEALERSSINC = CASE 
                    WHEN COCA_DEALERSTOTALES > 0 
                    THEN ROUND((COCA_DEALERSSONCRONIZADOS * 100.0 / COCA_DEALERSTOTALES), 2)
                    ELSE 0.00 
                END,
                FECHAMODIFICACION = :FechaModificacion,
                USUARIOMODIFICACION = :UsuarioModificacion
            WHERE COCA_CARGAARCHIVOSINID = :CargaArchivoSincronizacionId";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Creando registro de sincronización. DealerBac: {DealerBac}, CargaId: {CargaId}",
                entidad.DealerBac, entidad.CargaArchivoSincronizacionId);

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Obtener siguiente ID de secuencia
                var idSql = $"SELECT {SECUENCIA}.NEXTVAL FROM DUAL";
                var nuevoId = await connection.ExecuteScalarAsync<int>(idSql, transaction: transaction);
                entidad.SincArchivoDealerId = nuevoId;

                // Obtener datos de la carga
                var carga = await connection.QueryFirstOrDefaultAsync<dynamic>(sqlObtenerCarga, new
                {
                    CargaArchivoSincronizacionId = entidad.CargaArchivoSincronizacionId
                }, transaction: transaction);

            if (carga == null)
            {
                throw new NotFoundException(
                    $"No se encontró la carga de archivo con ID {entidad.CargaArchivoSincronizacionId}",
                    "CargaArchivoSincronizacion",
                    entidad.CargaArchivoSincronizacionId.ToString());
            }

            // ✅ Proceso y RegistrosSincronizados ya vienen de la carga en el Service
            // ✅ No se guarda COSA_PROCESO en la tabla (se obtiene mediante JOIN)
            // ✅ FechaSincronizacion y TokenConfirmacion ya se establecieron en el Service
            entidad.FechaAlta = DateTimeHelper.GetMexicoDateTime();
            entidad.UsuarioAlta = usuarioAlta;

                // Insertar registro (sin COSA_PROCESO)
                await connection.ExecuteAsync(sqlInsert, new
                {
                    SincArchivoDealerId = entidad.SincArchivoDealerId,
                    CargaArchivoSincronizacionId = entidad.CargaArchivoSincronizacionId,
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
                    CargaArchivoSincronizacionId = entidad.CargaArchivoSincronizacionId
                }, transaction: transaction);

                // Actualizar contadores en CO_CARGAARCHIVOSINCRONIZACION
                await connection.ExecuteAsync(sqlUpdateCarga, new
                {
                    DealersSincronizados = dealersSincronizados,
                    FechaModificacion = DateTimeHelper.GetMexicoDateTime(),
                    UsuarioModificacion = usuarioAlta,
                    CargaArchivoSincronizacionId = entidad.CargaArchivoSincronizacionId
                }, transaction: transaction);

                transaction.Commit();

                _logger.LogInformation("✅ [REPOSITORY] Registro creado exitosamente. ID: {Id}", entidad.SincArchivoDealerId);
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

