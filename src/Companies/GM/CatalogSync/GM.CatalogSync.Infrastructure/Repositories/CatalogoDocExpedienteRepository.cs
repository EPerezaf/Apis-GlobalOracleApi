using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.CatalogSync.Infrastructure.Repositories;

public class CatalogoDocExpedienteRepository : ICatalogoDocExpedienteRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ILogger<CatalogoDocExpedienteRepository> _logger;
    
    public CatalogoDocExpedienteRepository(
        IOracleConnectionFactory connectionFactory,
        ILogger<CatalogoDocExpedienteRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<(List<CatalogoDocExpediente> catalogo, int totalRecords)> GetByFilterAsync(
        int? claveTipoDocumento,
        int? idEmpleado,
        int? empresaId,
        int? idDocumento,
        string? estatusExpediente,
        string currentUser,
        string correlationId,
        int page,
        int pageSize)
    {
        try
        {
            _logger.LogInformation(
                "[{CorrelationId}] [REPOSITORY] Consultando catalogo - Clave Tipo Documento: {ClaveTipoDocumento}, Id Empleado: {IdEmpleado}, EmpresaId: {EmpresaId}, Estatus Expediente: {EstatusExpediente}, Pagina: {Page}",
                correlationId,
                claveTipoDocumento?.ToString() ?? "Todos", 
                idEmpleado?.ToString() ?? "Todos", 
                empresaId, 
                estatusExpediente ?? "Todos", 
                page);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (empresaId.HasValue)
            {
                whereClause += " AND EMPRESA_ID = :empresaId";
                parameters.Add("empresaId", empresaId);
                _logger.LogDebug("[{CorrelationId}] ✅ Aplicando filtro por empresa: {EmpresaId}",
                    correlationId, empresaId.Value);
            }

            if (claveTipoDocumento.HasValue)
            {
                whereClause += " AND CLAVE_TIPO_DOCUMENTO = :claveTipoDocumento";
                parameters.Add("claveTipoDocumento", claveTipoDocumento);
                _logger.LogDebug("[{CorrelationId}] ✅ Aplicando filtro por clave tipo documento: {ClaveTipoDocumento}",
                    correlationId, claveTipoDocumento.Value);
            }

            if (idEmpleado.HasValue && idEmpleado.Value > 0)
            {
                whereClause += " AND ID_EMPLEADO = :idEmpleado";
                parameters.Add("idEmpleado", idEmpleado);
                _logger.LogDebug("[{CorrelationId}] ✅ Aplicando filtro por id empleado: {IdEmpleado}",
                    correlationId, idEmpleado.Value);
            }

            if (idDocumento.HasValue)
            {
                whereClause += " AND ID_DOCUMENTO = :idDocumento";
                parameters.Add("idDocumento", idDocumento);
                _logger.LogDebug("[{CorrelationId}] ✅ Aplicando filtro por id documento: {IdDocumento}",
                    correlationId, idDocumento.Value);
            }

            if (!string.IsNullOrWhiteSpace(estatusExpediente))
            {
                whereClause += " AND ESTATUS_EXPEDIENTE = :estatusExpediente";
                parameters.Add("estatusExpediente", estatusExpediente);
                _logger.LogDebug("[{CorrelationId}] ✅ Aplicando filtro por estatus expediente: {EstatusExpediente}",
                    correlationId, estatusExpediente);
            }

            // Obtener total de registros 
            var countSql = $"SELECT COUNT(*) FROM LABGDMS.CO_VCAT_TIPO_DOCUMENTO {whereClause}";
            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            if (totalRecords == 0)
            {
                return (new List<CatalogoDocExpediente>(), 0);
            }

            int offset = (page - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", offset + pageSize);

            var sql = $@"
                SELECT * FROM (
                    SELECT 
                        EMPRESA_ID as EmpresaId,
                        ID_EMPLEADO as IdEmpleado,
                        CLAVE_TIPO_DOCUMENTO as ClaveTipoDocumento,
                        NOMBRE_TIPO_DOCUMENTO as NombreTipoDocumento,
                        OBLIGATORIO as Obligatorio,
                        ID_DOCUMENTO as IdDocumento,
                        NOMBRE_ARCHIVO_STORAGE as NombreArchivoStorage,
                        RUTA_STORAGE as RutaStorage,
                        CONTAINER_STORAGE as ContainerStorage,
                        OBSERVACIONES as Observaciones,
                        FECHA_CARGA as FechaCarga,
                        FECHA_VENCIMIENTO as FechaVencimiento,
                        ES_VIGENTE as EsVigente,
                        ESTATUS_EXPEDIENTE as EstatusExpediente,
                        EXISTE_ARCHIVO as ExisteArchivo,
                        ROW_NUMBER() OVER (ORDER BY ID_EMPLEADO) AS RNUM
                    FROM LABGDMS.CO_VCAT_TIPO_DOCUMENTO
                    {whereClause}
                ) WHERE RNUM > :offset AND RNUM <= :limit";

            var documentos = await connection.QueryAsync<CatalogoDocExpediente>(sql, parameters);

            _logger.LogInformation("[{CorrelationId}] [REPOSITORY] Consulta completada - {Count} registros de {Total} totales",
                correlationId, documentos.Count(), totalRecords);

            return (documentos.ToList(), totalRecords);
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error Oracle en GetByFilterAsync", correlationId);
            throw new DataAccessException("Error al consultar catálogo de documentos", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] [REPOSITORY] Error inesperado en GetByFilterAsync", correlationId);
            throw new DataAccessException("Error inesperado al consultar catálogo de documentos", ex);
        }
    }
}