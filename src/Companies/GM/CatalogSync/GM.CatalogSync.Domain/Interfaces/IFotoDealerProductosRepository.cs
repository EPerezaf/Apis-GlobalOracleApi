using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

/// <summary>
/// Interfaz del repositorio para Foto de Dealer Productos.
/// </summary>
public interface IFotoDealerProductosRepository
{
    /// <summary>
    /// Obtiene un registro por su ID.
    /// </summary>
    Task<FotoDealerProductos?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Obtiene todos los registros con filtros opcionales y paginación.
    /// </summary>
    /// <param name="cargaArchivoSincronizacionId">Filtro por ID de carga (opcional)</param>
    /// <param name="dealerBac">Filtro por código BAC del dealer (opcional)</param>
    /// <param name="dms">Filtro por sistema DMS (opcional)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Tupla con la lista de registros y el total de registros</returns>
    Task<(List<FotoDealerProductos> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        int? cargaArchivoSincronizacionId = null,
        string? dealerBac = null,
        string? dms = null,
        int page = 1,
        int pageSize = 200);

    /// <summary>
    /// Verifica si existe un registro con la combinación de cargaArchivoSincronizacionId y dealerBac.
    /// </summary>
    /// <param name="cargaArchivoSincronizacionId">ID de carga de archivo de sincronización</param>
    /// <param name="dealerBac">Código BAC del dealer</param>
    /// <returns>True si existe, False si no existe</returns>
    Task<bool> ExisteCombinacionAsync(int cargaArchivoSincronizacionId, string dealerBac);

    /// <summary>
    /// Verifica si existe un registro de carga de archivo de sincronización con el ID especificado.
    /// </summary>
    /// <param name="cargaArchivoSincronizacionId">ID de carga de archivo de sincronización</param>
    /// <returns>True si existe, False si no existe</returns>
    Task<bool> ExisteCargaArchivoSincronizacionIdAsync(int cargaArchivoSincronizacionId);

    /// <summary>
    /// Crea múltiples registros en batch usando transacción.
    /// </summary>
    /// <param name="entidades">Lista de entidades a crear</param>
    /// <param name="usuarioAlta">Usuario que realiza la operación</param>
    /// <returns>Lista de entidades creadas con IDs asignados</returns>
    Task<List<FotoDealerProductos>> CrearBatchAsync(
        List<FotoDealerProductos> entidades,
        string usuarioAlta);

    /// <summary>
    /// Obtiene un registro por ID con datos completos del JOIN (incluye FechaSincronizacion y TiempoSincronizacionHoras).
    /// </summary>
    Task<FotoDealerProductosMap?> ObtenerPorIdCompletoAsync(int id);

    /// <summary>
    /// Obtiene todos los registros con filtros y datos completos del JOIN (incluye FechaSincronizacion y TiempoSincronizacionHoras).
    /// </summary>
    /// <param name="cargaArchivoSincronizacionId">Filtro por ID de carga (opcional)</param>
    /// <param name="dealerBac">Filtro por código BAC del dealer (opcional)</param>
    /// <param name="dms">Filtro por sistema DMS (opcional)</param>
    /// <param name="sincronizado">Filtro por estado de sincronización: null=todos, 0=no sincronizados, 1=sincronizados (opcional)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    Task<(List<FotoDealerProductosMap> data, int totalRecords)> ObtenerTodosConFiltrosCompletoAsync(
        int? cargaArchivoSincronizacionId = null,
        string? dealerBac = null,
        string? dms = null,
        int? sincronizado = null,
        int page = 1,
        int pageSize = 200);
}

/// <summary>
/// Clase auxiliar para mapear resultados de JOIN con CO_CARGAARCHIVOSINCRONIZACION y CO_SINCRONIZACIONARCHIVOSDEALERS.
/// </summary>
public class FotoDealerProductosMap
{
    public int FotoDealerProductosId { get; set; }
    public int CargaArchivoSincronizacionId { get; set; }
    public string DealerBac { get; set; } = string.Empty;
    public string NombreDealer { get; set; } = string.Empty;
    public string RazonSocialDealer { get; set; } = string.Empty;
    public string Dms { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public DateTime FechaAlta { get; set; }
    public string UsuarioAlta { get; set; } = string.Empty;
    public DateTime? FechaModificacion { get; set; }
    public string? UsuarioModificacion { get; set; }
    public string? IdCarga { get; set; }
    public string? ProcesoCarga { get; set; }
    public DateTime? FechaCarga { get; set; }
    public DateTime? FechaSincronizacion { get; set; }
    public decimal? TiempoSincronizacionHoras { get; set; }
    public int Sincronizado { get; set; }
}

