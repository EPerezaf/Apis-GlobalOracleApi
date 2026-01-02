using GM.CatalogSync.Domain.Entities;

namespace GM.DealersSincronizacion.Domain.Interfaces;

/// <summary>
/// Interfaz del repositorio para Evento de Carga de Proceso (filtrado por dealer).
/// </summary>
public interface IEventoCargaProcesoRepository
{
    /// <summary>
    /// Obtiene un registro por su ID.
    /// </summary>
    Task<EventoCargaProceso?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Obtiene todos los registros con filtros opcionales y paginación.
    /// </summary>
    /// <param name="proceso">Filtro por proceso (opcional)</param>
    /// <param name="idCarga">Filtro por ID de carga (opcional)</param>
    /// <param name="actual">Filtro por estado actual (opcional)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Tupla con la lista de registros y el total de registros</returns>
    Task<(List<EventoCargaProceso> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        string? idCarga = null,
        bool? actual = null,
        int page = 1,
        int pageSize = 200);

    /// <summary>
    /// Obtiene el registro actual (actual=true) de evento de carga de proceso.
    /// </summary>
    Task<EventoCargaProceso?> ObtenerActualAsync();

    /// <summary>
    /// Obtiene el registro actual (actual=true) de evento de carga de proceso filtrado por proceso.
    /// </summary>
    /// <param name="proceso">Nombre del proceso (ej: "ProductList")</param>
    Task<EventoCargaProceso?> ObtenerActualPorProcesoAsync(string proceso);
}

