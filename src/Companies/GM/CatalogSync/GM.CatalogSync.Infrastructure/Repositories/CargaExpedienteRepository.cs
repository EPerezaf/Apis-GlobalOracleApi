using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GM.CatalogSync.Infrastructure.Repositories
{
    public class CargaExpedienteRepository : ICargaExpedienteRepository
    {
        private readonly IOracleConnectionFactory _connectionFactory;
        private readonly ILogger<CargaExpedienteRepository> _logger;

        public CargaExpedienteRepository(
            IOracleConnectionFactory connectionFactory,
            ILogger<CargaExpedienteRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        private async Task<int> ObtenerSiguienteIdAsync(IDbConnection connection)
        {
            const string sql = "SELECT NVL(MAX(COEE_IDDOCUMENTO), 0) + 1 FROM LABGDMS.CO_EMPLEADOS_EXPEDIENTE";
            return await connection.ExecuteScalarAsync<int>(sql);
        }

        public async Task<int> InsertarAsync(
            CargaExpediente expediente,
            string currentUser,
            string correlationId)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();
                
                //Calculamos el nuevo ID desde el Back (MAX + 1)
                int nuevoId = await ObtenerSiguienteIdAsync(connection);
                
                _logger.LogInformation(
                    "CorrelationId {CorrelationId} - Generado nuevo ID: {NuevoId} para empleado {IdEmpleado}",
                    correlationId,
                    nuevoId,
                    expediente.IdEmpleado);

                var sql = @"
                INSERT INTO LABGDMS.CO_EMPLEADOS_EXPEDIENTE
                (
                    EMPR_EMPRESAID,
                    COEE_IDDOCUMENTO,
                    COEM_IDEMPLEADO,
                    COCD_CLAVETIPODOCUMENTO,
                    COEE_NOMBREDOCUMENTO,
                    COEE_NOMBREARCHIVOSTORAGE,
                    COEE_RUTASTORAGE,
                    COEE_CONTAINERSTORAGE,
                    COEE_VERSIONDOCUMENTO,
                    COEE_ESVIGENTE,
                    COEE_FECHACARGA,
                    COEE_FECHADOCUMENTO,
                    COEE_FECHAVENCIMIENTO,
                    COEE_OBSERVACIONES,
                    USUARIOALTA,
                    FECHAALTA,
                    USUARIOMODIFICACION,
                    FECHAMODIFICACION
                )
                VALUES
                (
                    :EmpresaId,
                    :IdDocumento,
                    :IdEmpleado,
                    :ClaveTipoDocumento,
                    :NombreDocumento,
                    :NombreArchivoStorage,
                    :RutaStorage,
                    :ContainerStorage,
                    :VersionDocumento,
                    :EsVigente,
                    SYSDATE,
                    :FechaDocumento,
                    :FechaVencimiento,
                    :Observaciones,
                    :UsuarioAlta,
                    SYSDATE,
                    :UsuarioModificacion,
                    SYSDATE
                )";

                var parameters = new DynamicParameters();
                parameters.Add("EmpresaId", expediente.EmpresaId);
                parameters.Add("IdDocumento", nuevoId); // Pasamos el ID calculado
                parameters.Add("IdEmpleado", expediente.IdEmpleado);
                parameters.Add("ClaveTipoDocumento", expediente.ClaveTipoDocumento);
                parameters.Add("NombreDocumento", expediente.NombreDocumento);
                parameters.Add("NombreArchivoStorage", expediente.NombreArchivoStorage);
                parameters.Add("RutaStorage", expediente.RutaStorage);
                parameters.Add("ContainerStorage", expediente.ContainerStorage);
                parameters.Add("VersionDocumento", expediente.VersionDocumento);
                parameters.Add("EsVigente", expediente.EsVigente);
                parameters.Add("FechaDocumento", expediente.FechaDocumento);
                parameters.Add("FechaVencimiento", expediente.FechaVencimiento);
                parameters.Add("Observaciones", expediente.Observaciones);
                parameters.Add("UsuarioAlta", currentUser ?? "SYSTEM");
                parameters.Add("UsuarioModificacion", currentUser ?? "SYSTEM");

                await connection.ExecuteAsync(sql, parameters);

                _logger.LogInformation(
                    "CorrelationId {CorrelationId} - Expediente insertado exitosamente con ID {IdDocumento}",
                    correlationId,
                    nuevoId);

                return nuevoId;
            }
            catch (OracleException ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error Oracle en InsertarAsync", correlationId);
                throw new DataAccessException("Error al insertar documento en Oracle", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error inesperado en InsertarAsync", correlationId);
                throw new DataAccessException("Error inesperado al insertar documento", ex);
            }
        }

        public async Task<bool> ActualizarAsync(
            CargaExpediente expediente,
            string currentUser,
            string correlationId)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                using var transaction = connection.BeginTransaction();

                _logger.LogInformation(
                    "CorrelationId {CorrelationId} - Actualizando expediente {DocumentoId}",
                    correlationId,
                    expediente.IdDocumento);

                var sql = @"
                UPDATE LABGDMS.CO_EMPLEADOS_EXPEDIENTE
                SET
                    COEE_NOMBREARCHIVOSTORAGE = :NombreArchivoStorage,
                    COEE_RUTASTORAGE = :RutaStorage,
                    COEE_CONTAINERSTORAGE = :ContainerStorage,
                    COEE_VERSIONDOCUMENTO = :VersionDocumento,
                    COEE_FECHADOCUMENTO = :FechaDocumento,
                    COEE_FECHAVENCIMIENTO = :FechaVencimiento,
                    COEE_OBSERVACIONES = :Observaciones,
                    USUARIOMODIFICACION = :UsuarioModificacion,
                    FECHAMODIFICACION = SYSDATE
                WHERE COEE_IDDOCUMENTO = :IdDocumento";

                var rows = await connection.ExecuteAsync(sql, expediente);

                if (rows == 0)
                {
                    _logger.LogWarning(
                        "CorrelationId {CorrelationId} - No se encontró el expediente {DocumentoId}",
                        correlationId,
                        expediente.IdDocumento);
                    return false;
                }

                _logger.LogInformation(
                    "CorrelationId {CorrelationId} - Expediente {DocumentoId} actualizado",
                    correlationId,
                    expediente.IdDocumento);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "CorrelationId {CorrelationId} - Error al actualizar expediente {DocumentoId}",
                    correlationId,
                    expediente.IdDocumento);
                throw;
            }
        }

        public async Task<CargaExpediente?> ObtenerPorIdAsync(
            int idDocumento,
            string correlationId)
        {
            try
            {
                using var connection = await _connectionFactory.CreateConnectionAsync();

                _logger.LogInformation(
                    "CorrelationId {CorrelationId} - Obteniendo expediente {DocumentoId}",
                    correlationId,
                    idDocumento);

                var sql = @"
                SELECT 
                    EMPR_EMPRESAID AS EmpresaId,
                    COEE_IDDOCUMENTO AS IdDocumento,
                    COEM_IDEMPLEADO AS IdEmpleado,
                    COCD_CLAVETIPODOCUMENTO AS ClaveTipoDocumento,
                    COEE_NOMBREDOCUMENTO AS NombreDocumento,
                    COEE_NOMBREARCHIVOSTORAGE AS NombreArchivoStorage,
                    COEE_RUTASTORAGE AS RutaStorage,
                    COEE_CONTAINERSTORAGE AS ContainerStorage,
                    COEE_VERSIONDOCUMENTO AS VersionDocumento,
                    COEE_ESVIGENTE AS EsVigente,
                    COEE_FECHACARGA AS FechaCarga,
                    COEE_FECHADOCUMENTO AS FechaDocumento,
                    COEE_FECHAVENCIMIENTO AS FechaVencimiento,
                    COEE_OBSERVACIONES AS Observaciones,
                    USUARIOALTA AS UsuarioAlta,
                    FECHAALTA AS FechaAlta,
                    USUARIOMODIFICACION AS UsuarioModificacion,
                    FECHAMODIFICACION AS FechaModificacion
                FROM LABGDMS.CO_EMPLEADOS_EXPEDIENTE
                WHERE COEE_IDDOCUMENTO = :IdDocumento";

                var expediente = await connection.QueryFirstOrDefaultAsync<CargaExpediente>(
                    sql,
                    new { IdDocumento = idDocumento });

                if (expediente == null)
                {
                    _logger.LogWarning(
                        "CorrelationId {CorrelationId} - No se encontró el expediente {DocumentoId}",
                        correlationId,
                        idDocumento);
                }

                return expediente;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "CorrelationId {CorrelationId} - Error al obtener expediente {DocumentoId}",
                    correlationId,
                    idDocumento);
                throw;
            }
        }
    }
}