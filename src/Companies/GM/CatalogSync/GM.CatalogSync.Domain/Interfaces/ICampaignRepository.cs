using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

/// <summary>
/// Interfaz del repositorio de Campaign (definida en Domain)
/// </summary>
public interface ICampaignRepository
{
    Task<(List<Campaign> campaigns, int totalRecords)> GetByFiltersAsync(
        string? id,
        string? leadRecordType,
        int page,
        int pageSize,
        string correlationId);

    Task<int> GetTotalCountAsync(
        string? sourceCodeId,
        string? id,
        string? name,
        string correlationId);

    Task<bool> ExistsByIdAsync(
        string id,
        string correlationId);

    Task<int> InsertAsync(
        Campaign campaign,
        string currentUser,
        string correlationId);

    Task<int> UpsertBatchWithTransactionAsync(
        IEnumerable<Campaign> campaigns,
        string currentUser,
        string correlationId);

    Task<int> DeleteAllAsync(
        string currentUser,
        string correlationId);
}
