using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

public interface IDetalleRepository
{
    Task<(List<DetalleDealer> dealers, int totalRecords)>GetByFilterAsync(
        string? dealerId,
        string? nombre,
        string? razonSocial,
        string? rfc,
<<<<<<< HEAD
        int? noDealer,
=======
        int? empresaId,
>>>>>>> 49a386a (Se agrego el login y filtrado por empresa)
        int page,
        int pageSize,
        string currentUser,
        string correlationId);
    
}