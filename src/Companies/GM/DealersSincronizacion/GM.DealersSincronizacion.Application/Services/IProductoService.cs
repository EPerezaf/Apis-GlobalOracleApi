using GM.DealersSincronizacion.Application.DTOs;

namespace GM.DealersSincronizacion.Application.Services;

/// <summary>
/// Interfaz del servicio de productos para dealers.
/// </summary>
public interface IProductoService
{
    /// <summary>
    /// Obtiene todos los productos activos con paginación.
    /// </summary>
    Task<(List<ProductoDto> data, int totalRecords)> ObtenerTodosAsync(
        int page = 1,
        int pageSize = 200);
}

