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
        int? dealerId,
        string? curp,
        string? numeroEmpleado,
        int page,
        int pageSize,
        string correlationId)
    {
        try
        {
            _logger.LogInformation("[{CorrelationId}] 🗄️ [REPOSITORY] Consultando empleados - Id Empleado: {Idempleado}, Dealer Id: {DealerId}, Curp: {Curp}, Numero Empleado: {Numeroempleado} Página: {Page}",
                correlationId, idEmpleado.ToString() ?? "Todos", dealerId.ToString() ?? "Todas", curp ?? "Todos", numeroEmpleado ?? "Todos", page);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (idEmpleado.HasValue)
            {
                whereClause += " AND EMPR_EMPRESAID = :idEmpleado";
                parameters.Add("idEmpleado", idEmpleado);
            }

            if (dealerId.HasValue)
            {
                whereClause += " AND DEALERID = :dealerId";
                parameters.Add("dealerId", dealerId);
            }

            if (!string.IsNullOrWhiteSpace(curp))
            {
                whereClause += " AND COEM_CURP = :curp";
                parameters.Add("curp", curp);
            }

            if (!string.IsNullOrWhiteSpace(numeroEmpleado))
            {
                whereClause += " AND COEM_NUMEROEMPLEADO = :numeroEmpleado";
                parameters.Add("numeroEmpleado", numeroEmpleado);
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
                        COEM_IDEMPLEADO as IdEmpleado,
                        DEALERID as DealerId,
                        COEM_ACTIVO as Activo,
                        COEM_CURP as Curp,
                        COEM_NUMEROEMPLEADO as NumeroEmpleado,
                        COEM_NOMBRECOMPLETO as NombreCompleto,
                        COEH_DEPARTAMENTO as Departamento,
                        COEH_NOMBREPUESTO as NombrePuesto,
                        COEM_FECHANACIMIENTO as FechaNacimiento,
                        EDAD as Edad,
                        COEM_EMAILORGANIZACIONAL as EmailOgranizacional,
                        COED_TELEFONO as Telefono,
                        COEM_FECHAINGRESO as FechaIngreso,
                        JEFEINMEDIATO as JefeInmediato,
                        ROW_NUMBER() OVER (ORDER BY COEM_IDEMPLEADO) AS RNUM
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
        int? dealerId,
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
                whereClause += " AND EMPR_EMPRESAID = :idEmpleado";
                parameters.Add("idEmpleado", idEmpleado);
            }

            if (dealerId.HasValue)
            {
                whereClause += " AND DEALERID = :dealerId";
                parameters.Add("dealerId", dealerId);
            }

            if (!string.IsNullOrWhiteSpace(curp))
            {
                whereClause += " AND COEM_CURP = :curp";
                parameters.Add("curp", curp);
            }

            if (!string.IsNullOrWhiteSpace(numeroEmpleado))
            {
                whereClause += " AND COEM_NUMEROEMPLEADO = :numeroEmpleado";
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