using GM.DealersSincronizacion.Application.DTOs;

namespace GM.DealersSincronizacion.Application.Services;

/// <summary>
/// Interfaz del servicio de evento de carga de proceso para dealers.
/// </summary>
public interface IEventoCargaProcesoService
{
    /// <summary>
    /// Obtiene un registro por su ID.
    /// </summary>
    Task<EventoCargaProcesoDto?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Obtiene todos los registros con filtros opcionales y paginación.
    /// </summary>
    /// <param name="proceso">Filtro por proceso (opcional)</param>
    /// <param name="idCarga">Filtro por ID de carga (opcional)</param>
    /// <param name="actual">Filtro por estado actual (opcional)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Tupla con la lista de registros y el total de registros</returns>
    Task<(List<EventoCargaProcesoDto> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        string? idCarga = null,
        bool? actual = null,
        int page = 1,
        int pageSize = 200);

    /// <summary>
    /// Obtiene el registro actual (actual=true) de evento de carga de proceso.
    /// </summary>
    Task<EventoCargaProcesoActualDto?> ObtenerActualAsync();

    /// <summary>
    /// Obtiene el registro actual (actual=true) de evento de carga de proceso filtrado por proceso.
    /// </summary>
    /// <param name="proceso">Nombre del proceso (ej: "ProductList")</param>
    Task<EventoCargaProcesoActualDto?> ObtenerActualPorProcesoAsync(string proceso);
}

