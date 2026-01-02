using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

/// <summary>
/// Interfaz del repositorio para Sincronización de Carga de Proceso por Dealer.
/// </summary>
public interface ISincCargaProcesoDealerRepository
{
    /// <summary>
    /// Obtiene un registro por su ID.
    /// </summary>
    Task<SincCargaProcesoDealer?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Obtiene todos los registros con filtros opcionales y paginación.
    /// </summary>
    /// <param name="proceso">Filtro por proceso (opcional)</param>
    /// <param name="eventoCargaProcesoId">Filtro por ID de evento de carga de proceso (opcional)</param>
    /// <param name="dealerBac">Filtro por código BAC del dealer (opcional)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Tupla con la lista de registros y el total de registros</returns>
    Task<(List<SincCargaProcesoDealer> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        int? eventoCargaProcesoId = null,
        string? dealerBac = null,
        int page = 1,
        int pageSize = 200);

    /// <summary>
    /// Verifica si existe un registro con la combinación proceso, eventoCargaProcesoId y dealerBac.
    /// </summary>
    Task<bool> ExisteRegistroAsync(string proceso, int eventoCargaProcesoId, string dealerBac);

    /// <summary>
    /// Obtiene un registro existente por proceso, eventoCargaProcesoId y dealerBac (para validación de duplicados).
    /// </summary>
    Task<SincCargaProcesoDealer?> ObtenerPorProcesoCargaYDealerAsync(string proceso, int eventoCargaProcesoId, string dealerBac);

    /// <summary>
    /// Verifica si existe un registro de evento de carga de proceso con el ID especificado.
    /// </summary>
    Task<bool> ExisteEventoCargaProcesoIdAsync(int eventoCargaProcesoId);

    /// <summary>
    /// Crea un nuevo registro de sincronización.
    /// </summary>
    /// <param name="entidad">Entidad a crear</param>
    /// <param name="usuarioAlta">Usuario que realiza la operación</param>
    /// <returns>Entidad creada con ID asignado</returns>
    Task<SincCargaProcesoDealer> CrearAsync(SincCargaProcesoDealer entidad, string usuarioAlta);
}

