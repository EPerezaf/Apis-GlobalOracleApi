using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

/// <summary>
/// Interfaz del repositorio para Sincronización de Archivos por Dealer.
/// </summary>
public interface ISincArchivoDealerRepository
{
    /// <summary>
    /// Obtiene un registro por su ID.
    /// </summary>
    Task<SincArchivoDealer?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Obtiene todos los registros con filtros opcionales y paginación.
    /// </summary>
    /// <param name="proceso">Filtro por proceso (opcional)</param>
    /// <param name="cargaArchivoSincronizacionId">Filtro por ID de carga de archivo de sincronización (opcional)</param>
    /// <param name="dealerBac">Filtro por código BAC del dealer (opcional)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Tupla con la lista de registros y el total de registros</returns>
    Task<(List<SincArchivoDealer> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        int? cargaArchivoSincronizacionId = null,
        string? dealerBac = null,
        int page = 1,
        int pageSize = 200);

    /// <summary>
    /// Verifica si existe un registro con la combinación proceso, cargaArchivoSincronizacionId y dealerBac.
    /// </summary>
    Task<bool> ExisteRegistroAsync(string proceso, int cargaArchivoSincronizacionId, string dealerBac);

    /// <summary>
    /// Obtiene un registro existente por proceso, cargaArchivoSincronizacionId y dealerBac (para validación de duplicados).
    /// </summary>
    Task<SincArchivoDealer?> ObtenerPorProcesoCargaYDealerAsync(string proceso, int cargaArchivoSincronizacionId, string dealerBac);

    /// <summary>
    /// Verifica si existe un registro de carga de archivo de sincronización con el ID especificado.
    /// </summary>
    Task<bool> ExisteCargaArchivoSincronizacionIdAsync(int cargaArchivoSincronizacionId);

    /// <summary>
    /// Crea un nuevo registro de sincronización.
    /// </summary>
    /// <param name="entidad">Entidad a crear</param>
    /// <param name="usuarioAlta">Usuario que realiza la operación</param>
    /// <returns>Entidad creada con ID asignado</returns>
    Task<SincArchivoDealer> CrearAsync(SincArchivoDealer entidad, string usuarioAlta);
}

