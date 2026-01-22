using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

public interface IAsignacionRepository
{
    Task<(List<Asignacion> asignacion, int totalRecords)> GetByFilterAsync(
        string? usuario,
        //string? dealer,
        int page, 
        int pageSize,
        string correlationId);

    Task<(List<Asignacion> disponibles, int totalRecords)> GetUsuarioDisponibleByFilterAsync(
        string? userId,
        string? nombre, string? email,
        int? empresaId,
        int page,
        int pageSize,
        string correlationId,
        string currentUser
    );

    Task<(List<DetalleDealer> disponibles, int totalRecords)> GetDealerDisponibleByFilterAsync(
        string? userId,
        int? empresaId,
        int page,
        int pageSize,
        string currentUser,
        string correlationId);
    
    Task<int> GetTotalCountAsync(
        string? usuario,
        string? dealer,
        string correlationId);
    
    Task<int> InsertAsync(
        Asignacion asignacion,
        string currentUser,
        string correlationId);
    
    Task<int>UpsertBatchWithTransactionAsync(
        IEnumerable<Asignacion> asignacion,
        string currentUser,
        string correlationId);
    
    Task<int> DeleteAllAsync(
        string usuario,
        string dealer,
        string currentUser,
        string correlationId);
    
}