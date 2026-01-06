using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Services
{
    /// <summary>
    /// Interfaz del servicio de Campanias
    /// </summary>
    public interface ICampaignService
    {
        Task<(List<CampaignResponseDto> data, int totalRecords)> GetCampaignsAsync(
            string? id,
            string? leadRecordType,
            int page,
            int pageSize,
            string currentUser,
            string correlationId);

        Task<CampaignBatchResultDto> ProcessBatchInsertAsync(
            List<CreateCampaignDto> campaigns,
            string currentUser,
            string correlationId);

        Task<int> DeleteAllAsync(
            string currentUser,
            string correlationId);

    }
}

