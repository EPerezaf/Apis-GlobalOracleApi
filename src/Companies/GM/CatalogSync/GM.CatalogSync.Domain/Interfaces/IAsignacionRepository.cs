using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

public interface IAsignacionRepository
{
    Task<(List<Asignacion> asignacion, int totalRecords)> GetByFilterAsync(
        string? usuario,
        string? dealer,
        int page, 
        int pageSize,
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