using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.CatalogSync.Infrastructure.Repositories;

public class EmpleadoRepository : IEmpleadoRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<EmpleadoRepository> _logger;
    public EmpleadoRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<EmpleadoRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;    
    }
    

    public async Task<(List<Empleado> empleados, int totalRecords)> GetByFilterAsync(
        int? idEmpleado,
        string? dealerId,
        string? curp,
        int? activo,
        int? empresaId,
        int page,
        int pageSize,
        string correlationId)
    {
        try
        {
            _logger.LogInformation("[{CorrelationId}] 🗄️ [REPOSITORY] Consultando empleados - Id Empleado: {Idempleado}, Dealer Id: {DealerId}, Curp: {Curp}, Página: {Page}",
                correlationId, idEmpleado.ToString() ?? "Todos", dealerId ?? "Todas", curp ?? "Todos", page);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (empresaId.HasValue)
            {
                whereClause += " AND EMPR_EMPRESAID = :empresaId";
                parameters.Add("empresaId", empresaId);
                _logger.LogDebug("[{CorrelationId}] ✅ Aplicando filtro por empresa: {EmpresaId}", 
                    correlationId, empresaId.Value);
            }

            if (idEmpleado.HasValue)
            {
                whereClause += " AND ID_EMPLEADO = :idEmpleado";
                parameters.Add("idEmpleado", idEmpleado);
            }

            if (!string.IsNullOrWhiteSpace(dealerId))
            {
                whereClause += " AND DEALERID = :dealerId";
                parameters.Add("dealerId", dealerId);
            }

            if (!string.IsNullOrWhiteSpace(curp))
            {
                whereClause += " AND CURP = :curp";
                parameters.Add("curp", curp);
            }

            
            if(activo.HasValue)
            {
                whereClause += " AND ACTIVO = :activo";
                parameters.Add("activo", activo);
            }

            // Obtener total de registros
            var countSql = $"SELECT COUNT(*) FROM LABGDMS.CO_VEMPLEADOS {whereClause}";
            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            if (totalRecords == 0)
            {
                return (new List<Empleado>(), 0);
            }

            int offset = (page - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", offset + pageSize);

            var sql = $@"
                SELECT * FROM (
                    SELECT 
                           EMPR_EMPRESAID as EmpresaId,
                            ID_EMPLEADO as IdEmpleado,
                            DEALERID as DealerId,
                            ACTIVO as Activo,
                            CURP as Curp,
                            NUMERO_EMPLEADO as NumeroEmpleado,
                            NOMBRE as Nombre,
                            PRIMER_APELLIDO as PrimerApellido,
                            SEGUNDO_APELLIDO as SegundoApellido,
                            DEPARTAMENTO as Departamento,
                            PUESTO as Puesto,
                            FECHA_NACIMIENTO as FechaNacimiento,
                            EDAD as Edad,
                            EMAIL_ORGANIZACIONAL as EmailOrganizacional,
                            TELEFONO as Telefono,
                            FECHA_INGRESO as FechaIngreso,
                            JEFE_NOMBRE as JefeNombre,
                            JEFE_PRIMER_APELLIDO as JefePrimerApellido,
                            JEFE_SEGUNDO_APELLIDO as JefeSegundoApellido,
                            ANTIGUEDAD as Antiguedad,
                        ROW_NUMBER() OVER (ORDER BY ID_EMPLEADO) AS RNUM
                    FROM LABGDMS.CO_VEMPLEADOS
                    {whereClause}
                ) WHERE RNUM > :offset AND RNUM <= :limit";
            
            var empleados = await connection.QueryAsync<Empleado>(sql, parameters);
            _logger.LogInformation("[{CorrelationId}] ✅ [REPOSITORY] Consulta completada - {Count} registros de {Total} totales",
                correlationId, empleados.Count(), totalRecords);

            return (empleados.ToList(), totalRecords);
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error Oracle en GetByFiltersAsync", correlationId);
            throw new DataAccessException("Error al consultar empleados", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error inesperado en GetByFiltersAsync", correlationId);
            throw new DataAccessException("Error inesperado al consultar empleados", ex);
        }
    }

    public async Task<int> GetTotalCountAsync(
        int? idEmpleado,
        string? dealerId,
        string? curp,
        string? numeroEmpleado,
        string correlationId)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (idEmpleado.HasValue)
            {
                whereClause += " AND ID_EMPLEADO = :idEmpleado";
                parameters.Add("idEmpleado", idEmpleado);
            }

            if (!string.IsNullOrWhiteSpace(dealerId))
            {
                whereClause += " AND DEALERID = :dealerId";
                parameters.Add("dealerId", dealerId);
            }

            if (!string.IsNullOrWhiteSpace(curp))
            {
                whereClause += " AND CURP = :curp";
                parameters.Add("curp", curp);
            }

            if (!string.IsNullOrWhiteSpace(numeroEmpleado))
            {
                whereClause += " AND NUMERO_EMPLEADO = :numeroEmpleado";
                parameters.Add("numeroEmpleado", numeroEmpleado);
            }

            var sql = $"SELECT COUNT(*) FROM LABGDMS.CO_VEMPLEADOS {whereClause}";
            var count = await connection.ExecuteScalarAsync<int>(sql, parameters);

            return count;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error Oracle en GetTotalCountAsync", correlationId);
            throw new DataAccessException("Error al contar empleados", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] ❌ [REPOSITORY] Error inesperado en GetTotalCountAsync", correlationId);
            throw new DataAccessException("Error inesperado al contar empleados", ex);
        } 
    }


    
    
}