using Dapper;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Shared.Exceptions;
using Shared.Infrastructure;

namespace GM.CatalogSync.Infrastructure.Repositories;

/// <summary>
/// Clase auxiliar para mapear resultados de JOIN con CO_CARGAARCHIVOSINCRONIZACION.
/// </summary>
internal class SincArchivoDealerMap
{
    public int SincArchivoDealerId { get; set; }
    public string Proceso { get; set; } = string.Empty;
    public int CargaArchivoSincronizacionId { get; set; }
    public string DmsOrigen { get; set; } = string.Empty;
    public string DealerBac { get; set; } = string.Empty;
    public string NombreDealer { get; set; } = string.Empty;
    public DateTime FechaSincronizacion { get; set; }
    public int RegistrosSincronizados { get; set; }
    public DateTime FechaAlta { get; set; }
    public string UsuarioAlta { get; set; } = string.Empty;
    public DateTime? FechaModificacion { get; set; }
    public string? UsuarioModificacion { get; set; }
    public string? IdCarga { get; set; }
    public string? ProcesoCarga { get; set; }
    public DateTime? FechaCarga { get; set; }
    public decimal? TiempoSincronizacionHoras { get; set; }
}

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
                s.COSA_SINCARCHIVODEALERID as SincArchivoDealerId,
                s.COSA_PROCESO as Proceso,
                s.COSA_COCA_CARGAARCHIVOSINID as CargaArchivoSincronizacionId,
                s.COSA_DMSORIGEN as DmsOrigen,
                s.COSA_DEALERBAC as DealerBac,
                s.COSA_NOMBREDEALER as NombreDealer,
                s.COSA_FECHASINCRONIZACION as FechaSincronizacion,
                s.COSA_REGISTROSSINCRONIZADOS as RegistrosSincronizados,
                s.FECHAALTA as FechaAlta,
                s.USUARIOALTA as UsuarioAlta,
                s.FECHAMODIFICACION as FechaModificacion,
                s.USUARIOMODIFICACION as UsuarioModificacion,
                c.COCA_IDCARGA as IdCarga,
                c.COCA_PROCESO as ProcesoCarga,
                c.COCA_FECHACARGA as FechaCarga,
                ROUND((s.COSA_FECHASINCRONIZACION - c.COCA_FECHACARGA) * 24, 2) as TiempoSincronizacionHoras
            FROM CO_SINCRONIZACIONARCHIVOSDEALERS s
            INNER JOIN CO_CARGAARCHIVOSINCRONIZACION c ON s.COSA_COCA_CARGAARCHIVOSINID = c.COCA_CARGAARCHIVOSINID
            WHERE s.COSA_SINCARCHIVODEALERID = :Id";

        try
        {
            _logger.LogInformation("🗄️ [REPOSITORY] Obteniendo registro de sincronización por ID: {Id}", id);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var resultado = await connection.QueryFirstOrDefaultAsync<SincArchivoDealerMap>(sql, new { Id = id });

            if (resultado == null)
            {
                _logger.LogWarning("⚠️ [REPOSITORY] Registro de sincronización con ID {Id} no encontrado", id);
                return null;
            }

            // Mapear a entidad (sin los campos del JOIN)
            var entidad = new SincArchivoDealer
            {
                SincArchivoDealerId = resultado.SincArchivoDealerId,
                Proceso = resultado.Proceso,
                CargaArchivoSincronizacionId = resultado.CargaArchivoSincronizacionId,
                DmsOrigen = resultado.DmsOrigen,
                DealerBac = resultado.DealerBac,
                NombreDealer = resultado.NombreDealer,
                FechaSincronizacion = resultado.FechaSincronizacion,
                RegistrosSincronizados = resultado.RegistrosSincronizados,
                FechaAlta = resultado.FechaAlta,
                UsuarioAlta = resultado.UsuarioAlta,
                FechaModificacion = resultado.FechaModificacion,
                UsuarioModificacion = resultado.UsuarioModificacion
            };

            // Guardar datos del JOIN en un campo adicional (usando un diccionario o clase auxiliar)
            // Por ahora, los datos del JOIN se obtendrán en el servicio mediante una consulta adicional
            _logger.LogInformation("✅ [REPOSITORY] Registro de sincronización con ID {Id} obtenido exitosamente", id);
            return entidad;
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
        int? cargaArchivoSincronizacionId = null,
        string? dealerBac = null,
        int page = 1,
        int pageSize = 200)
    {
        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Consultando registros de sincronización - Proceso: {Proceso}, CargaArchivoSincronizacionId: {CargaArchivoSincronizacionId}, DealerBac: {DealerBac}, Página: {Page}, PageSize: {PageSize}",
                proceso ?? "Todos", cargaArchivoSincronizacionId?.ToString() ?? "Todos", dealerBac ?? "Todos", page, pageSize);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var parameters = new DynamicParameters();
            var whereClause = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(proceso))
            {
                whereClause += " AND UPPER(COSA_PROCESO) LIKE UPPER(:Proceso)";
                parameters.Add("Proceso", $"%{proceso}%");
            }

            if (cargaArchivoSincronizacionId.HasValue)
            {
                whereClause += " AND COSA_COCA_CARGAARCHIVOSINID = :CargaArchivoSincronizacionId";
                parameters.Add("CargaArchivoSincronizacionId", cargaArchivoSincronizacionId.Value);
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
                        s.COSA_SINCARCHIVODEALERID as SincArchivoDealerId,
                        s.COSA_PROCESO as Proceso,
                        s.COSA_COCA_CARGAARCHIVOSINID as CargaArchivoSincronizacionId,
                        s.COSA_DMSORIGEN as DmsOrigen,
                        s.COSA_DEALERBAC as DealerBac,
                        s.COSA_NOMBREDEALER as NombreDealer,
                        s.COSA_FECHASINCRONIZACION as FechaSincronizacion,
                        s.COSA_REGISTROSSINCRONIZADOS as RegistrosSincronizados,
                        s.FECHAALTA as FechaAlta,
                        s.USUARIOALTA as UsuarioAlta,
                        s.FECHAMODIFICACION as FechaModificacion,
                        s.USUARIOMODIFICACION as UsuarioModificacion,
                        c.COCA_IDCARGA as IdCarga,
                        c.COCA_PROCESO as ProcesoCarga,
                        c.COCA_FECHACARGA as FechaCarga,
                        ROUND((s.COSA_FECHASINCRONIZACION - c.COCA_FECHACARGA) * 24, 2) as TiempoSincronizacionHoras,
                        ROW_NUMBER() OVER (ORDER BY s.COSA_FECHASINCRONIZACION DESC) AS RNUM
                    FROM {TABLA} s
                    INNER JOIN CO_CARGAARCHIVOSINCRONIZACION c ON s.COSA_COCA_CARGAARCHIVOSINID = c.COCA_CARGAARCHIVOSINID
                    {whereClause}
                ) WHERE RNUM > :offset AND RNUM <= :limit";

            var resultados = await connection.QueryAsync<SincArchivoDealerMap>(sql, parameters);
            var lista = resultados.Select(r => new SincArchivoDealer
            {
                SincArchivoDealerId = r.SincArchivoDealerId,
                Proceso = r.Proceso,
                CargaArchivoSincronizacionId = r.CargaArchivoSincronizacionId,
                DmsOrigen = r.DmsOrigen,
                DealerBac = r.DealerBac,
                NombreDealer = r.NombreDealer,
                FechaSincronizacion = r.FechaSincronizacion,
                RegistrosSincronizados = r.RegistrosSincronizados,
                FechaAlta = r.FechaAlta,
                UsuarioAlta = r.UsuarioAlta,
                FechaModificacion = r.FechaModificacion,
                UsuarioModificacion = r.UsuarioModificacion
            }).ToList();

            // Guardar datos del JOIN en un diccionario para acceso posterior
            // Los datos del JOIN se obtendrán en el servicio mediante una consulta adicional

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
    public async Task<bool> ExisteRegistroAsync(string proceso, int cargaArchivoSincronizacionId, string dealerBac)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM CO_SINCRONIZACIONARCHIVOSDEALERS 
            WHERE COSA_PROCESO = :Proceso 
            AND COSA_COCA_CARGAARCHIVOSINID = :CargaArchivoSincronizacionId
            AND COSA_DEALERBAC = :DealerBac";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Verificando existencia de registro - Proceso: {Proceso}, CargaArchivoSincronizacionId: {CargaArchivoSincronizacionId}, DealerBac: {DealerBac}",
                proceso, cargaArchivoSincronizacionId, dealerBac);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var count = await connection.ExecuteScalarAsync<int>(sql, new 
            { 
                Proceso = proceso, 
                CargaArchivoSincronizacionId = cargaArchivoSincronizacionId, 
                DealerBac = dealerBac 
            });

            var existe = count > 0;
            _logger.LogInformation(
                "✅ [REPOSITORY] Registro (Proceso: '{Proceso}', CargaArchivoSincronizacionId: {CargaArchivoSincronizacionId}, DealerBac: '{DealerBac}') existe: {Existe}",
                proceso, cargaArchivoSincronizacionId, dealerBac, existe);

            return existe;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al verificar existencia. ErrorCode: {ErrorCode}",
                ex.Number);
            throw new DataAccessException("Error al acceder a la base de datos", ex);
        }
    }

    /// <summary>
    /// Verifica si existe un registro de carga de archivo de sincronización con el ID especificado.
    /// </summary>
    public async Task<bool> ExisteCargaArchivoSincronizacionIdAsync(int cargaArchivoSincronizacionId)
    {
        const string sql = @"
            SELECT COUNT(1) 
            FROM CO_CARGAARCHIVOSINCRONIZACION 
            WHERE COCA_CARGAARCHIVOSINID = :CargaArchivoSincronizacionId
            AND COCA_ACTUAL = 1";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Verificando existencia de CargaArchivoSincronizacionId: {CargaArchivoSincronizacionId}",
                cargaArchivoSincronizacionId);

            using var connection = await _connectionFactory.CreateConnectionAsync();

            var count = await connection.ExecuteScalarAsync<int>(sql, new 
            { 
                CargaArchivoSincronizacionId = cargaArchivoSincronizacionId
            });

            var existe = count > 0;
            _logger.LogInformation(
                "✅ [REPOSITORY] CargaArchivoSincronizacionId {CargaArchivoSincronizacionId} existe: {Existe}",
                cargaArchivoSincronizacionId, existe);

            return existe;
        }
        catch (OracleException ex)
        {
            _logger.LogError(ex, "❌ [REPOSITORY] Error Oracle al verificar CargaArchivoSincronizacionId. ErrorCode: {ErrorCode}",
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
                COSA_COCA_CARGAARCHIVOSINID,
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
                :CargaArchivoSincronizacionId,
                :DmsOrigen,
                :DealerBac,
                :NombreDealer,
                :FechaSincronizacion,
                :RegistrosSincronizados,
                SYSDATE,
                :UsuarioAlta
            ) RETURNING COSA_SINCARCHIVODEALERID INTO :Id";

        // SQL para obtener COCA_DEALERSTOTALES a partir de CargaArchivoSincronizacionId
        const string sqlObtenerCarga = @"
            SELECT 
                COCA_CARGAARCHIVOSINID as CargaArchivoSincronizacionId,
                COCA_DEALERSTOTALES as DealersTotales
            FROM CO_CARGAARCHIVOSINCRONIZACION
            WHERE COCA_CARGAARCHIVOSINID = :CargaArchivoSincronizacionId
            AND COCA_ACTUAL = 1";

        // SQL para contar dealers sincronizados
        const string sqlContarDealers = @"
            SELECT COUNT(*)
            FROM CO_SINCRONIZACIONARCHIVOSDEALERS
            WHERE COSA_COCA_CARGAARCHIVOSINID = :CargaArchivoSincronizacionId";

        try
        {
            _logger.LogInformation(
                "🗄️ [REPOSITORY] Iniciando creación de registro de sincronización con actualización automática de contadores. Proceso: {Proceso}, CargaArchivoSincronizacionId: {CargaArchivoSincronizacionId}, DealerBac: {DealerBac}, Usuario: {Usuario}",
                entidad.Proceso, entidad.CargaArchivoSincronizacionId, entidad.DealerBac, usuarioAlta);

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Validar que existe el CargaArchivoSincronizacionId y obtener DealersTotales
                var cargaInfo = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    sqlObtenerCarga,
                    new { CargaArchivoSincronizacionId = entidad.CargaArchivoSincronizacionId },
                    transaction);

                if (cargaInfo == null)
                {
                    _logger.LogWarning(
                        "⚠️ [REPOSITORY] No se encontró registro de carga con CargaArchivoSincronizacionId: {CargaArchivoSincronizacionId} y COCA_ACTUAL=1",
                        entidad.CargaArchivoSincronizacionId);
                    throw new NotFoundException(
                        $"No se encontró un registro de carga activo con CargaArchivoSincronizacionId {entidad.CargaArchivoSincronizacionId}",
                        "CargaArchivoSincronizacion",
                        entidad.CargaArchivoSincronizacionId.ToString());
                }

                int cargaArchivoSincronizacionId = cargaInfo.CargaArchivoSincronizacionId;
                int dealersTotales = cargaInfo.DealersTotales;

                _logger.LogInformation(
                    "📊 [REPOSITORY] Carga encontrada. COCA_CARGAARCHIVOSINID: {CargaId}, DealersTotales: {DealersTotales}",
                    cargaArchivoSincronizacionId, dealersTotales);

                // 2. Insertar registro de sincronización
                var parametersInsert = new DynamicParameters();
                parametersInsert.Add("Proceso", entidad.Proceso);
                parametersInsert.Add("CargaArchivoSincronizacionId", entidad.CargaArchivoSincronizacionId);
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
                    new { CargaArchivoSincronizacionId = entidad.CargaArchivoSincronizacionId },
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

