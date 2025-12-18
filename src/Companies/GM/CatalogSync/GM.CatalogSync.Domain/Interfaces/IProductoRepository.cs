using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

/// <summary>
/// Interfaz del repositorio de Producto (definida en Domain)
/// </summary>
public interface IProductoRepository
{
    Task<(List<Producto> productos, int totalRecords)> GetByFiltersAsync(
        string? pais,
        string? marcaNegocio,
        int? anioModelo,
        int page,
        int pageSize,
        string correlationId);

    Task<int> GetTotalCountAsync(
        string? pais,
        string? marcaNegocio,
        int? anioModelo,
        string correlationId);

    Task<bool> ExistsByProductoAnioAndLocalAsync(
        string nombreProducto,
        int anioModelo,
        string? nombreLocal,
        string correlationId);

    Task<int> InsertAsync(
        Producto producto,
        string currentUser,
        string correlationId);

    Task<int> UpsertBatchWithTransactionAsync(
        IEnumerable<Producto> productos,
        string currentUser,
        string correlationId);

    Task<int> DeleteAllAsync(
        string currentUser,
        string correlationId);
}

