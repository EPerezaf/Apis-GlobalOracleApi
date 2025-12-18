using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Services
{
    /// <summary>
    /// Interfaz del servicio de Product
    /// </summary>
    public interface IProductService
    {
        Task<(List<ProductResponseDto> data, int totalRecords)> GetProductsAsync(
            string? pais,
            string? marcaNegocio,
            int? anioModelo,
            int page,
            int pageSize,
            string currentUser,
            string correlationId);

        Task<ProductBatchResultDto> ProcessBatchInsertAsync(
            List<ProductCreateDto> products,
            string currentUser,
            string correlationId);

        Task<int> DeleteAllAsync(
            string currentUser,
            string correlationId);
    }
}

