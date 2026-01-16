namespace GM.DealerSync.Domain.Interfaces;

/// <summary>
/// Interfaz para obtener campañas desde CO_GM_CAMPAIGNCATALOG para generar payload
/// </summary>
public interface ICampaignPayloadRepository
{
    /// <summary>
    /// Obtiene todas las campañas activas desde CO_GM_CAMPAIGNCATALOG
    /// </summary>
    Task<List<CampaignPayload>> GetAllCampaignsAsync();
}

/// <summary>
/// Entidad para campañas en el payload
/// </summary>
public class CampaignPayload
{
    public string? SourceCodeId { get; set; }
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? RecordTypeId { get; set; }
    public string? LeadRecordType { get; set; }
    public string? LeadEnquiryType { get; set; }
    public string? LeadSource { get; set; }
    public string? LeadSourceDetails { get; set; }
    public string? Status { get; set; }
}

