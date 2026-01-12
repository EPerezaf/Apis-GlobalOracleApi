using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

public interface ICampañaRepository
{
    Task<(List<Campaña> campañas, int totalRecords)> GetByFilterAsync(
        string? sourceCodeId,
        string? id,
        string? name,
        int page,
        int pageSize,
        string correlationId);

    Task<int> GetTotalCountAsync(
        string? sourceCodeId,
        string? id,
        string? name,
        string correlationId);
    
    Task<int> InsertAsync(
        Campaña campaña,
        string currentUser,
        string correlationId);
    
    Task<int> UpsertBatchWithTransactionAsync(
        IEnumerable<Campaña> campañas,
        string currentUser,
        string correlationId);
    
    Task<int> DeleteAllAsync(
        string currentUser,
        string correlationId);
    
    
}