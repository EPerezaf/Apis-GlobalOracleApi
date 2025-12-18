using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Services;

/// <summary>
/// Interfaz del servicio de Producto
/// </summary>
public interface IProductoService
{
    Task<(List<ProductoRespuestaDto> data, int totalRecords)> ObtenerProductosAsync(
        string? pais,
        string? marcaNegocio,
        int? anioModelo,
        int page,
        int pageSize,
        string currentUser,
        string correlationId);

    Task<ProductoBatchResultadoDto> ProcesarBatchInsertAsync(
        List<ProductoCrearDto> productos,
        string currentUser,
        string correlationId);

    Task<int> EliminarTodosAsync(
        string currentUser,
        string correlationId);
}

