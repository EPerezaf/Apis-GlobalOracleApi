using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.DealersSincronizacion.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.DealersSincronizacion.Infrastructure.Repositories;

/// <summary>
/// Repository para acceso a datos de Carga de Archivo de Sincronización usando Dapper (para dealers).
/// </summary>
public class CargaArchivoSincRepository : ICargaArchivoSincRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<CargaArchivoSincRepository> _logger;

    public CargaArchivoSincRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<CargaArchivoSincRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CargaArchivoSincronizacion?> ObtenerActualAsync()
    {
        const string sql = @"
            SELECT 
                COCA_CARGAARCHIVOSINID as CargaArchivoSincronizacionId,
                COCA_PROCESO as Proceso,
                COCA_NOMBREARCHIVO as NombreArchivo,
                COCA_FECHACARGA as FechaCarga,
                COCA_IDCARGA as IdCarga,
                COCA_REGISTROS as Registros,
                COCA_ACTUAL as Actual,
                COCA_DEALERSTOTALES as DealersTotales,
                COCA_DEALERSSONCRONIZADOS as DealersSincronizados,
                COCA_PORCDEALERSSINC as PorcDealersSinc,
                COCA_TABLARELACION as TablaRelacion,
                FECHAALTA as FechaAlta,
                USUARIOALTA as UsuarioAlta,
                FECHAMODIFICACION as FechaModificacion,
                USUARIOMODIFICACION as UsuarioModificacion
            FROM CO_CARGAARCHIVOSINCRONIZACION
            WHERE COCA_ACTUAL = 1
            AND ROWNUM = 1
            ORDER BY COCA_CARGAARCHIVOSINID DESC";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo registro actual de carga de archivo de sincronización");

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<CargaArchivoSincronizacion>(sql);

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] No se encontró registro actual de carga de archivo");
                return null;
            }

            _logger.LogInformation("✅ [REPOSITORY] Registro actual obtenido exitosamente. ID: {Id}", resultado.CargaArchivoSincronizacionId);
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
    public async Task<CargaArchivoSincronizacion?> ObtenerActualPorProcesoAsync(string proceso)
    {
        const string sql = @"
            SELECT 
                COCA_CARGAARCHIVOSINID as CargaArchivoSincronizacionId,
                COCA_PROCESO as Proceso,
                COCA_NOMBREARCHIVO as NombreArchivo,
                COCA_FECHACARGA as FechaCarga,
                COCA_IDCARGA as IdCarga,
                COCA_REGISTROS as Registros,
                COCA_ACTUAL as Actual,
                COCA_DEALERSTOTALES as DealersTotales,
                COCA_DEALERSSONCRONIZADOS as DealersSincronizados,
                COCA_PORCDEALERSSINC as PorcDealersSinc,
                COCA_TABLARELACION as TablaRelacion,
                FECHAALTA as FechaAlta,
                USUARIOALTA as UsuarioAlta,
                FECHAMODIFICACION as FechaModificacion,
                USUARIOMODIFICACION as UsuarioModificacion
            FROM CO_CARGAARCHIVOSINCRONIZACION
            WHERE COCA_ACTUAL = 1
            AND UPPER(COCA_PROCESO) = UPPER(:Proceso)
            AND ROWNUM = 1
            ORDER BY COCA_CARGAARCHIVOSINID DESC";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo registro actual de carga de archivo de sincronización para proceso: {Proceso}", proceso);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<CargaArchivoSincronizacion>(sql, new { Proceso = proceso });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] No se encontró registro actual de carga de archivo para proceso: {Proceso}", proceso);
                return null;
            }

            _logger.LogInformation("✅ [REPOSITORY] Registro actual obtenido exitosamente para proceso {Proceso}. ID: {Id}", proceso, resultado.CargaArchivoSincronizacionId);
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
