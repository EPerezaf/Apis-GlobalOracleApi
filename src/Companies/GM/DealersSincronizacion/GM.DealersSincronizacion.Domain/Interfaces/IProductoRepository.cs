using GM.CatalogSync.Domain.Entities;

namespace GM.DealersSincronizacion.Domain.Interfaces;

/// <summary>
/// Interfaz del repositorio de Producto (filtrado por dealer).
/// </summary>
public interface IProductoRepository
{
    /// <summary>
    /// Obtiene todos los productos (sin filtros adicionales para dealers).
    /// </summary>
    Task<(List<Producto> productos, int totalRecords)> ObtenerTodosAsync(
        int page = 1,
        int pageSize = 200);
}





