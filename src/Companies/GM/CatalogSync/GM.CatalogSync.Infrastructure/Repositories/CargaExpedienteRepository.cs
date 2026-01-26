using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GM.CatalogSync.Infrastructure.Repositories
{
    public class CargaExpedienteRepository : ICargaExpedienteRepository
    {
        private readonly IDbConnection _connection;
        private readonly ILogger<CargaExpedienteRepository> _logger;

        public CargaExpedienteRepository(
            IDbConnection connection,
            ILogger<CargaExpedienteRepository> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        public async Task<int> InsertarAsync(
            CargaExpediente expediente,
            string correlationId)
        {
            try
            {
                _logger.LogInformation(
                    "CorrelationId {CorrelationId} - Insertando expediente empleado {EmpleadoId}",
                    correlationId,
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
                    FECHAALTA
                )
                VALUES
                (
                    :EmpresaId,
                    SEQ_COEE_IDDOCUMENTO.NEXTVAL,
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
                    SYSDATE
                )
                RETURNING COEE_IDDOCUMENTO INTO :IdDocumento";

                var parameters = new DynamicParameters(expediente);
                parameters.Add("IdDocumento", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _connection.ExecuteAsync(sql, parameters);

                var idDocumento = parameters.Get<int>("IdDocumento");
                
                _logger.LogInformation(
                    "CorrelationId {CorrelationId} - Expediente insertado con ID {IdDocumento}",
                    correlationId,
                    idDocumento);

                return idDocumento;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "CorrelationId {CorrelationId} - Error al insertar expediente {EmpleadoId}",
                    correlationId,
                    expediente.IdEmpleado);
                throw;
            }
        }

        public async Task<bool> ActualizarAsync(
            CargaExpediente expediente,
            string correlationId)
        {
            try
            {
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

                var rows = await _connection.ExecuteAsync(sql, expediente);

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

                var expediente = await _connection.QueryFirstOrDefaultAsync<CargaExpediente>(
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