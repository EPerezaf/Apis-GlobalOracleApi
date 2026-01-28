using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.CatalogSync.Infrastructure.Repositories;

public class EmpleadoExpedienteRepository : IEmpleadoExpedienteRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<EmpleadoExpedienteRepository> _logger;
    public EmpleadoExpedienteRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<EmpleadoExpedienteRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;    
    }

    public async Task<(List<EmpleadoExpediente> empleadosExpediente, int totalRecords)> GetByFilterAsync(
        int? idDocumento,
        int? idEmpleado,
        int? claveTipoDocumento,
        int? empresaId,
        int page,
        int pageSize,
        string correlationId,
        string currentUser)
    {
        try
        {
            _logger.LogInformation("[{CorrelationId}] [REPOSITORY] Consultando empleados expediente - Id Documento: {IdDocumento}, Id Empleado: {IdEmpleado}, Clave Tipo Documento: {ClaveTipoDocumento}, Empresa Id: {EmpresaId}",
            correlationId,
            idDocumento.ToString() ?? "Todos",
            idEmpleado.ToString() ?? "Todos",
            claveTipoDocumento.ToString() ?? "Todos",
            empresaId.ToString() ?? "Todos");

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (empresaId.HasValue)
            {
                whereClause += " AND EMPR_EMPRESAID = :empresaId";
                parameters.Add("empresaId", empresaId);
            }

            if(idDocumento.HasValue)
            {
                whereClause += " AND ID_DOCUMENTO = :idDocumento";
                parameters.Add("idDocumento", idDocumento);
            }

            if (idEmpleado.HasValue)
            {
                whereClause += " AND ID_EMPLEADO = :idEmpleado";
                parameters.Add("idEmpleado", idEmpleado);
            }

            if (claveTipoDocumento.HasValue)
            {
                whereClause += " AND CLAVE_TIPO_DOCUMENTO = :claveTipoDocumento";
                parameters.Add("claveTipoDocumento", claveTipoDocumento);
            }

            var countSql = $@"SELECT COUNT(*) FROM LABGDMS.CO_VEMPLEADOS_EXPEDIENTE {whereClause}";
            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            if(totalRecords == 0)
            {
                return (new List<EmpleadoExpediente>(), 0);
            }

            var offset = (page - 1)* pageSize;
            parameters.Add("offset", offset);
            parameters.Add("pageSize", pageSize);

            var sql = $@"
                SELECT * FROM (
                    SELECT
                        EMPR_EMPRESAID as EmpresaId,
                        ID_DOCUMENTO as IdDocumento,
                        ID_EMPLEADO as IdEmpleado,
                        NUMERO_EMPLEADO as NumeroEmpleado,
                        NOMBRE as Nombre,
                        PRIMER_APELLIDO as PrimerApellido,
                        SEGUNDO_APELLIDO as SegundoApellido,
                        CLAVE_TIPO_DOCUMENTO as ClaveTipoDocumento,
                        NOMBRE_TIPO_DOCUMENTO as NombreTipoDocumento,
                        NOMBRE_DOCUMENTO as NombreDocumento,
                        NOMBRE_ARCHIVO_STORAGE as NombreArchivoStorage,
                        RUTA_STORAGE as RutaStorage,
                        DESCRIPCION as Descripcion,
                        OBLIGATORIO as Obligatorio,
                        REQUIERE_VIGENCIA as RequiereVigencia,
                        CONTAINER_STORAGE as ContainerStorage,
                        VERSION_DOCUMENTO as VersionDocumento,
                        ES_VIGENTE as EsVigente,
                        FECHA_CARGA as FechaCarga,
                        FECHA_DOCUMENTO as FechaDocumento,
                        FECHA_VENCIMIENTO as FechaVencimiento,
                        OBSERVACIONES as Observaciones,
                    ROW_NUMBER() OVER (ORDER BY ID_DOCUMENTO) AS RNUM
                FROM LABGDMS.CO_VEMPLEADOS_EXPEDIENTE
                {whereClause}
            ) WHERE RNUM > :offset AND RNUM <= :pageSize";

            /*var sql = $@"
                SELECT * FROM (
                    SELECT
                        EMPR_EMPRESAID as EmpresaId,
                        COEE_IDDOCUMENTO as IdDocumento,
                        COEM_IDEMPLEADO as IdEmpleado,
                        COCD_CLAVETIPODOCUMENTO as ClaveTipoDocumento,
                        COEE_NOMBREDOCUMENTO as NombreDocumento,
                        COEE_NOMBREARCHIVOSTORAGE as NombreArchivoStorage,
                        COEE_RUTASTORAGE as RutaStorage,
                        COEE_CONTAINERSTORAGE as ContainerStorage,
                        COEE_VERSIONDOCUMENTO as VersionDocumento,
                        COEE_ESVIGENTE as EsVigente,
                        COEE_FECHACARGA as FechaCarga,
                        COEE_FECHADOCUMENTO as FechaDocumento,
                        COEE_OBSERVACIONES as Observaciones,
                        USUARIOALTA as UsuarioAlta, 
                        FECHAALTA AS FechaAlta,
                        USUARIOMODIFICACION as UsuarioModificacion,
                        FECHAMODIFICACION as FechaModificacion,
                    ROW_NUMBER() OVER (ORDER BY COEE_IDDOCUMENTO)AS RNUM
                FROM LABGDMS.CO_EMPLEADOS_EXPEDIENTE
                {whereClause}
            ) WHERE RNUM > :offset AND RNUM <= :pageSize";*/

            var empleadosExpediente = await connection.QueryAsync<EmpleadoExpediente>(sql, parameters);
            _logger.LogInformation("[{CorrelationId}] [REPOSITORY] Consulta completada - {Count} registros de {Total} totales",
            correlationId, empleadosExpediente.Count(), totalRecords);

            return (empleadosExpediente.ToList(), totalRecords);
        }
        catch(OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error Oracle en GetByFilterAsync", correlationId);
            throw new DataAccessException("Error al obtener empleados por expediente", ex);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error inesperado en GetByFilterAsync", correlationId);
            throw new DataAccessException("Error al obtener empleados por expediente", ex);
        }
    }
    
    
}