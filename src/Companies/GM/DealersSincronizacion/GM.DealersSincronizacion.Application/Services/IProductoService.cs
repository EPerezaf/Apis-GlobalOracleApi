using GM.DealersSincronizacion.Application.DTOs;

namespace GM.DealersSincronizacion.Application.Services;

/// <summary>
/// Interfaz del servicio de productos para dealers.
/// </summary>
public interface IProductoService
{
    /// <summary>
    /// Obtiene todos los productos activos con paginación e información de carga de archivo de sincronización.
    /// </summary>
    Task<(ProductosConCargaDto data, int totalRecords)> ObtenerTodosConCargaAsync(
        int page = 1,
        int pageSize = 200);
}

