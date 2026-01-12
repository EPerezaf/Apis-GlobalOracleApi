namespace GM.CatalogSync.Domain.Entities;

public class Campaña
{
    public int CampañaId { get; set;}

    public string SourceCodeId { get; set;} = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Name { get;set;} = string.Empty;
    public string RecordTypeId {get;set;} = string.Empty;
    public string LeadRecordType {get;set;} = string.Empty;
    public string? LeadEnquiryType {get;set;}
    public string? LeadSource { get;set;}
    public string? LeadSourceDetails { get;set;}
    public string? Status { get;set;}
    public DateTime? FechaAlta {get;set;}
    public string? UsuarioAlta {get;set;}
    public DateTime FechaModificacion {get;set;}
    public string? UsuarioModificacion {get;set;}
}