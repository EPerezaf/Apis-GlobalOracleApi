using System.ComponentModel.DataAnnotations;

namespace GM.CatalogSync.Application.DTOs;

public class CampañaCrearDto
{
    [Required]
    public string SourceCodeId { get; set; } = string.Empty;
    [Required]
    public string Id {get; set;} = string.Empty;
    [Required]
    public string Name {get; set;} = string.Empty;
    [Required]
    public string RecordTypeId { get; set; } = string.Empty;
    [Required]
    public string LeadRecordType { get; set; } = string.Empty;
    public string? LeadEnquiryType { get; set;} 
    public string? LeadSource { get; set;} 
    public string? LeadSourceDetails { get; set; }
    public string? Status { get; set;}

}

public class CampañaRespuestDto
{
    public int CampañaId { get; set;}
    public string SourceCodeId { get; set; } = string.Empty;
    public string Id {get; set;} = string.Empty;
    public string Name {get; set;} = string.Empty;
    public string RecordTypeId { get; set; } = string.Empty;
    public string LeadRecordType { get; set; } = string.Empty;
    public string? LeadEnquiryType { get; set;} 
    public string? LeadSource { get; set;} 
    public string? LeadSourceDetails { get; set; }
    public string? Status { get; set;}
    public DateTime? FechaAlta { get; set;}
    public string? UsuarioAlta { get; set;}
    public DateTime? FechaModificacion { get; set;}
    public string? UsuarioModificacion { get; set;}
}
public class CampañaBatchRequestDto
{
    [Required]
    public List<CampañaCrearDto> Json { get; set; } = new();
}

public class CampañaBatchResultadoDto
{
    public int RegistrosTotales { get; set;}
    public int RegistrosInsertados { get; set; }
    public int RegistrosActualizados { get; set; }
    public int RegistrosError { get; set;}
    public int OmitidosPorError { get; set;}
}