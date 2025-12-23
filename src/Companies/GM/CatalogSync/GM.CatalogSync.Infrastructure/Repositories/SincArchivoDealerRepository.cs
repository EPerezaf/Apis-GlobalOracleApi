using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.CatalogSync.Infrastructure.Repositories;

/// <summary>
/// Repository para acceso a datos de Sincronización de Archivos por Dealer usando Dapper.
/// Tabla: CO_SINCRONIZACIONARCHIVOSDEALERS
/// </summary>
public class SincArchivoDealerRepository : ISincArchivoDealerRepository
{
    private readonly IOracleConnectionFactory _connectionFactory;
    private readonly ICargaArchivoSincRepository _cargaArchivoSincRepository;
    private readonly ILogger<SincArchivoDealerRepository> _logger;

    private const string TABLA = "CO_SINCRONIZACIONARCHIVOSDEALERS";
    private const string SECUENCIA = "SEQ_COSA_SINCARCHIVODEALERID";

    public SincArchivoDealerRepository(
        IOracleConnectionFactory connectionFactory,
        ICargaArchivoSincRepository cargaArchivoSincRepository,
        ILogger<SincArchivoDealerRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _cargaArchivoSincRepository = cargaArchivoSincRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SincArchivoDealer?> ObtenerPorIdAsync(int id)
    {
        const string sql = @"
            SELECT 
                COSA_SINCARCHIVODEALERID as SincArchivoDealerId,
                COSA_PROCESO as Proceso,
                COSA_IDCARGA as IdCarga,
                COSA_DMSORIGEN as DmsOrigen,
                COSA_DEALERBAC as DealerBac,
                COSA_NOMBREDEALER as NombreDealer,
                COSA_FECHASINCRONIZACION as FechaSincronizacion,
                COSA_REGISTROSSINCRONIZADOS as RegistrosSincronizados,
                FECHAALTA as FechaAlta,
                USUARIOALTA as UsuarioAlta,
                FECHAMODIFICACION as FechaModificacion,
                USUARIOMODIFICACION as UsuarioModificacion
            FROM CO_SINCRONIZACIONARCHIVOSDEALERS
            WHERE COSA_SINCARCHIVODEALERID = :Id";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo registro de sincronización por ID: {Id}", id);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<SincArchivoDealer>(sql, new { Id = id });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] Registro de sincronización con ID {Id} no encontrado", id);
                return null;
            }

            _logger.LogInformation("✅ [REPOSITORY] Registro de sincronización con ID {Id} obtenido exitosamente", id);
            return resultado;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al obtener registro por ID {Id}. ErrorCode: {ErrorCode}",
                id, ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<(List<SincArchivoDealer> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        string? idCarga = null,
        string? dealerBac = null,
        int page = 1,
        int pageSize = 200)
    {
        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Consultando registros de sincronización - Proceso: {Proceso}, IdCarga: {IdCarga}, DealerBac: {DealerBac}, Página: {Page}, PageSize: {PageSize}",
                proceso ?? "Todos", idCarga ?? "Todos", dealerBac ?? "Todos", page, pageSize);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(proceso))
            {
                whereClause += " AND UPPER(COSA_PROCESO) LIKE UPPER(:Proceso)";
                parameters.Add("Proceso", $"%{proceso}%");
            }

            if (!string.IsNullOrWhiteSpace(idCarga))
            {
                whereClause += " AND UPPER(COSA_IDCARGA) LIKE UPPER(:IdCarga)";
                parameters.Add("IdCarga", $"%{idCarga}%");
            }

            if (!string.IsNullOrWhiteSpace(dealerBac))
            {
                whereClause += " AND UPPER(COSA_DEALERBAC) LIKE UPPER(:DealerBac)";
                parameters.Add("DealerBac", $"%{dealerBac}%");
            }

            // Obtener total de registros
            var countSql = $"SELECT COUNT(*) FROM {TABLA} {whereClause}";
            var totalRecords = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            if (totalRecords == 0)
            {
                _logger.LogInformation("✅ [REPOSITORY] No se encontraron registros de sincronización");
                return (new List<SincArchivoDealer>(), 0);
            }

            // Aplicar paginación
            int offset = (page - 1) * pageSize;
            parameters.Add("offset", offset);
            parameters.Add("limit", offset + pageSize);

            var sql = $@"
                SELECT * FROM (
                    SELECT 
                        COSA_SINCARCHIVODEALERID as SincArchivoDealerId,
                        COSA_PROCESO as Proceso,
                        COSA_IDCARGA as IdCarga,
                        COSA_DMSORIGEN as DmsOrigen,
                        COSA_DEALERBAC as DealerBac,
                        COSA_NOMBREDEALER as NombreDealer,
                        COSA_FECHASINCRONIZACION as FechaSincronizacion,
                        COSA_REGISTROSSINCRONIZADOS as RegistrosSincronizados,
                        FECHAALTA as FechaAlta,
                        USUARIOALTA as UsuarioAlta,
                        FECHAMODIFICACION as FechaModificacion,
                        USUARIOMODIFICACION as UsuarioModificacion,
                        ROW_NUMBER() OVER (ORDER BY COSA_FECHASINCRONIZACION DESC) AS RNUM
                    FROM {TABLA}
                    {whereClause}
                ) WHERE RNUM > :offset AND RNUM <= :limit";

            var resultados = await connection.QueryAsync<SincArchivoDealer>(sql, parameters);
            var lista = resultados.ToList();

            _logger.LogInformation("✅ [REPOSITORY] Se obtuvieron {Cantidad} registros de sincronización de {Total} totales (Página {Page})", 
                lista.Count, totalRecords, page);
            return (lista, totalRecords);
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al obtener registros con filtros. ErrorCode: {ErrorCode}",
                ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExisteRegistroAsync(string proceso, string idCarga, string dealerBac)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM CO_SINCRONIZACIONARCHIVOSDEALERS 
            WHERE COSA_PROCESO = :Proceso 
            AND COSA_IDCARGA = :IdCarga
            AND COSA_DEALERBAC = :DealerBac";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Verificando existencia de registro - Proceso: {Proceso}, IdCarga: {IdCarga}, DealerBac: {DealerBac}",
                proceso, idCarga, dealerBac);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var count = await connection.ExecuteScalarAsync<int>(sql, new 
            { 
                Proceso = proceso, 
                IdCarga = idCarga, 
                DealerBac = dealerBac 
            });

            var existe = count > 0;
            _logger.LogInformation(
                "✅ [REPOSITORY] Registro (Proceso: '{Proceso}', IdCarga: '{IdCarga}', DealerBac: '{DealerBac}') existe: {Existe}",
                proceso, idCarga, dealerBac, existe);

            return existe;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al verificar existencia. ErrorCode: {ErrorCode}",
                ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <inheritdoc />
    public async Task<SincArchivoDealer> CrearAsync(SincArchivoDealer entidad, string usuarioAlta)
    {
        const string sqlInsert = @"
            INSERT INTO CO_SINCRONIZACIONARCHIVOSDEALERS (
                COSA_SINCARCHIVODEALERID,
                COSA_PROCESO,
                COSA_IDCARGA,
                COSA_DMSORIGEN,
                COSA_DEALERBAC,
                COSA_NOMBREDEALER,
                COSA_FECHASINCRONIZACION,
                COSA_REGISTROSSINCRONIZADOS,
                FECHAALTA,
                USUARIOALTA
            ) VALUES (
                SEQ_COSA_SINCARCHIVODEALERID.NEXTVAL,
                :Proceso,
                :IdCarga,
                :DmsOrigen,
                :DealerBac,
                :NombreDealer,
                :FechaSincronizacion,
                :RegistrosSincronizados,
                SYSDATE,
                :UsuarioAlta
            ) RETURNING COSA_SINCARCHIVODEALERID INTO :Id";

        // SQL para obtener COCA_CARGAARCHIVOSINID y COCA_DEALERSTOTALES a partir de IdCarga
        const string sqlObtenerCarga = @"
            SELECT 
                COCA_CARGAARCHIVOSINID as CargaArchivoSincronizacionId,
                COCA_DEALERSTOTALES as DealersTotales
            FROM CO_CARGAARCHIVOSINCRONIZACION
            WHERE COCA_IDCARGA = :IdCarga
            AND COCA_ACTUAL = 1";

        // SQL para contar dealers sincronizados
        const string sqlContarDealers = @"
            SELECT COUNT(*)
            FROM CO_SINCRONIZACIONARCHIVOSDEALERS
            WHERE COSA_IDCARGA = :IdCarga";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Iniciando creación de registro de sincronización con actualización automática de contadores. Proceso: {Proceso}, IdCarga: {IdCarga}, DealerBac: {DealerBac}, Usuario: {Usuario}",
                entidad.Proceso, entidad.IdCarga, entidad.DealerBac, usuarioAlta);

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Obtener COCA_CARGAARCHIVOSINID y COCA_DEALERSTOTALES a partir de IdCarga
                var cargaInfo = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    sqlObtenerCarga,
                    new { IdCarga = entidad.IdCarga },
                    transaction);

                if (cargaInfo == null)
                {
                    _logger.LogWarning(
                        "⚠️ [REPOSITORY] No se encontró registro de carga con IdCarga: {IdCarga} y COCA_ACTUAL=1",
                        entidad.IdCarga);
                    throw new NotFoundException(
                        $"No se encontró un registro de carga activo con IdCarga '{entidad.IdCarga}'",
                        "CargaArchivoSincronizacion",
                        entidad.IdCarga);
                }

                int cargaArchivoSincronizacionId = cargaInfo.CargaArchivoSincronizacionId;
                int dealersTotales = cargaInfo.DealersTotales;

                _logger.LogInformation(
                    "📊 [REPOSITORY] Carga encontrada. COCA_CARGAARCHIVOSINID: {CargaId}, DealersTotales: {DealersTotales}",
                    cargaArchivoSincronizacionId, dealersTotales);

                // 2. Insertar registro de sincronización
                var parametersInsert = new DynamicParameters();
                parametersInsert.Add("Proceso", entidad.Proceso);
                parametersInsert.Add("IdCarga", entidad.IdCarga);
                parametersInsert.Add("DmsOrigen", entidad.DmsOrigen);
                parametersInsert.Add("DealerBac", entidad.DealerBac);
                parametersInsert.Add("NombreDealer", entidad.NombreDealer);
                parametersInsert.Add("FechaSincronizacion", entidad.FechaSincronizacion);
                parametersInsert.Add("RegistrosSincronizados", entidad.RegistrosSincronizados);
                parametersInsert.Add("UsuarioAlta", usuarioAlta);
                parametersInsert.Add("Id", dbType: System.Data.DbType.Int32, direction: System.Data.ParameterDirection.Output);

                await connection.ExecuteAsync(sqlInsert, parametersInsert, transaction);

                var nuevoId = parametersInsert.Get<int>("Id");

                _logger.LogInformation(
                    "✅ [REPOSITORY] Registro de sincronización creado. ID: {Id}",
                    nuevoId);

                // 3. Contar dealers sincronizados (incluyendo el recién insertado)
                var dealersSincronizados = await connection.ExecuteScalarAsync<int>(
                    sqlContarDealers,
                    new { IdCarga = entidad.IdCarga },
                    transaction);

                // 4. Calcular porcentaje
                decimal porcDealersSinc = 0.00m;
                if (dealersTotales > 0)
                {
                    porcDealersSinc = Math.Round((decimal)dealersSincronizados / dealersTotales * 100, 2);
                }

                _logger.LogInformation(
                    "📊 [REPOSITORY] Contadores calculados. DealersSincronizados: {DealersSinc}, DealersTotales: {DealersTotales}, PorcDealersSinc: {Porc}%",
                    dealersSincronizados, dealersTotales, porcDealersSinc);

                // 5. Actualizar contadores en CO_CARGAARCHIVOSINCRONIZACION usando el repositorio correspondiente
                var filasActualizadas = await _cargaArchivoSincRepository.ActualizarContadoresDealersAsync(
                    cargaArchivoSincronizacionId,
                    dealersSincronizados,
                    porcDealersSinc,
                    usuarioAlta,
                    transaction);

                if (filasActualizadas == 0)
                {
                    _logger.LogWarning(
                        "⚠️ [REPOSITORY] No se actualizó ningún registro de carga. COCA_CARGAARCHIVOSINID: {CargaId}",
                        cargaArchivoSincronizacionId);
                }
                else
                {
                    _logger.LogInformation(
                        "✅ [REPOSITORY] Contadores actualizados en CO_CARGAARCHIVOSINCRONIZACION. COCA_CARGAARCHIVOSINID: {CargaId}",
                        cargaArchivoSincronizacionId);
                }

                // 6. Commit de la transacción
                transaction.Commit();

                _logger.LogInformation(
                    "✅ [REPOSITORY] Transacción completada exitosamente. Registro ID: {Id}, Contadores actualizados: DealersSincronizados={DealersSinc}, PorcDealersSinc={Porc}%",
                    nuevoId, dealersSincronizados, porcDealersSinc);

                // Obtener el registro creado
                var registroCreado = await ObtenerPorIdAsync(nuevoId);

                if (registroCreado == null)
                {
                    throw new DataAccessException("No se pudo obtener el registro recién creado");
                }

                return registroCreado;
            }
            catch (Exception)
            {
                // Rollback en caso de error
                transaction.Rollback();
                _logger.LogError("❌ [REPOSITORY] Rollback ejecutado debido a error en la transacción");
                throw;
            }
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex,
                "❌ [REPOSITORY] Error Oracle al crear registro. Proceso: {Proceso}, DealerBac: {DealerBac}, ErrorCode: {ErrorCode}",
                entidad.Proceso, entidad.DealerBac, ex.Number);
            throw new DataAccessException("Error al crear el registro en la base de datos", ex);
        }
    }
}

