using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.CatalogSync.Infrastructure.Repositories;

public class EmpleadoHistoricoRepository : IEmpleadoHistoricoRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<EmpleadoHistoricoRepository> _logger;
    public EmpleadoHistoricoRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<EmpleadoHistoricoRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }
    
    public async Task<(List<EmpleadoHistorico> empleadosHistorico, int totalRecords)> GetByFilterAsync(
        int? idAsignacion,
        int? idEmpleado,
        int? dealerId,
        string? clavePuesto,
        string? departamento,
        int? esActual,
        int? empresaId,
        int page,
        int pageSize,
        string currentUser,
        string correlationId)
    {
        try
        {
            _logger.LogInformation("[{CorrelationId}] [REPOSITORY] Consultando empleados historico - Id ASignacion: {Idasignacion},Id Empleado: {IdEmpleado}, Dealer Id: {DealerId}, Clave Puesto: {ClavePuesto}, Departamento: {Departamento}, Es Actual: {EsActual}, Empresa Id: {EmpresaId}, Pagina: {Page} ",
            correlationId,
            idAsignacion.ToString() ?? "Todos",
            idEmpleado.ToString() ?? "Todos",
            dealerId.ToString() ?? "Todos",
            clavePuesto ?? "Todos",
            departamento ?? "Todos",
            esActual.ToString() ?? "Todos",
            empresaId.ToString() ?? "Todos",
            page);

            using var connection = await  _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (empresaId.HasValue)
            {
                whereClause += " AND EMPR_EMPRESAID = :empresaId";
                parameters.Add("empresaId", empresaId);
                _logger.LogDebug("[{CorrelationId}] Aplicando filtro por empresa: {EmpresaId}",
                correlationId, empresaId.Value);
            }

            if (idAsignacion.HasValue)
            {
                whereClause += " AND COEH_IDASIGNACIONPUESTO = :id";
                parameters.Add("id", idAsignacion);
            }
            if (idEmpleado.HasValue)
            {
                whereClause += " AND COEM_IDEMPLEADO = :idEmpleado";
                parameters.Add("idEmpleado", idEmpleado);
            }
            if (dealerId.HasValue)
            {
                whereClause += " AND DEALERID = :dealerId";
                parameters.Add("dealerId", dealerId);
            }
            if (!string.IsNullOrWhiteSpace(clavePuesto))
            {
                whereClause += " AND COEH_CLAVEPUESTO = :clavePuesto";
                parameters.Add("clavePuesto", clavePuesto);
            }
            if (!string.IsNullOrWhiteSpace(departamento))
            {
                whereClause += " AND COEH_DEPARTAMENTO = :departamento";
                parameters.Add("departamento", departamento);
            }

            //Obtener total de registros
            var countSql = $"SELECT COUNT(*) FROM LABGDMS.CO_EMPLEADOS_HIST_PUESTOS {whereClause}";
            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            if(totalRecords == 0)
            {
                return (new List<EmpleadoHistorico>(),0);
            }

            int offset = (page - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", offset + pageSize);

            var sql = $@"
                SELECT * FROM (
                    SELECT 
                            EMPR_EMPRESAID as EmpresaId,
                            COEH_IDASIGNACIONPUESTO as IdAsignacionPuesto,
                            COEM_IDEMPLEADO as IdEmpleado,
                            DEALERID as DealerId,
                            COEH_CLAVEPUESTO as ClavePuesto, 
                            COEH_NOMBREPUESTO as NombrePuesto,
                            COEH_DEPARTAMENTO as Departamento,
                            COEM_IDEMPLEADOJEFE as IdEmpeladoJefe,
                            COEH_FECHAINICIOASIGNACION as FechaInicioAsignacion,
                            COEH_FECHAFINASIGNACION as FechaFinAsignacion,
                            COEH_ESACTUAL as EsActual,
                            COEH_MOTIVOCAMBIO as MotivoCambio,
                            COEH_OBSERVACIONES as Observaciones,
                            USUARIOALTA as UsuarioAlta,
                            FECHAALTA as FechaAlta,
                            USUARIOMODIFICACION as UsuarioModifica,
                            FECHAMODIFICACION as FechaModifica,
                        ROW_NUMBER() OVER (ORDER BY COEH_IDASIGNACIONPUESTO) AS RNUM
                    FROM LABGDMS.CO_EMPLEADOS_HIST_PUESTOS
                    {whereClause}
                ) WHERE RNUM > :offset AND RNUM <= :limit";
            
            var empleadosHistorico = await connection.QueryAsync<EmpleadoHistorico>(sql, parameters);
            _logger.LogInformation("[{CorrelationId}] [REPOSITORY] Consulta completada - {Count} registrso de {Total} totales",
            correlationId, empleadosHistorico.Count(), totalRecords);

            return (empleadosHistorico.ToList(), totalRecords);
        }    
        catch(OracleException ex)
        {
            _logger.LogError(ex,
            "[{CorrelationId}] [REPOSITORY] Error Oracle en GetByFilterAsync", correlationId);
            throw new DataAccessException("Errro al consultar empleados historico", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
            "[{CorrelationId}] [REPOSITORY] Error inesperado en GetByFilterAsync", correlationId);
            throw new DataAccessException("Error inesperado al consultar empleados historcio", ex);
        }
        
    }
    
}