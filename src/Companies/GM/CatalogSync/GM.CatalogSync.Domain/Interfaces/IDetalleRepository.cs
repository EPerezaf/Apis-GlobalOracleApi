using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

public interface IDetalleRepository
{
    Task<(List<DetalleDealer> dealers, int totalRecords)>GetByFilterAsync(
        string? dealerId,
        string? nombre,
        string? razonSocial,
        string? rfc,
        int? noDealer,
        int page,
        int pageSize,
        string currentUser,
        string correlationId);
    
}