using System.ComponentModel.DataAnnotations;

namespace GM.CatalogSync.Application.DTOs;

public class AsignacionCrearDto
{
    [Required]
    public string Usuario { get; set; } = string.Empty;

    [Required]
    public string Dealer { get; set; } = string.Empty;
}

public class AsignacionRespuestaDto
{
    public string Usuario { get; set; } = string.Empty;
    public string Dealer { get; set;} = string.Empty;
    public DateTime? FechaAlta { get; set;}
    public string? UsuarioAlta {get; set;} = string.Empty;
    public DateTime? FechaModificacion {get; set;}
    public string? UsuarioModificacion { get; set;} = string.Empty;
    public int EmpresaId {get; set;}
}

public class AsignacionBatchRequestDto
{
    [Required]
    public List<AsignacionCrearDto> Json { get; set;} = new();
}

public class AsignacionBatchResultadoDto
{
    public int RegistrosTotales { get; set;}
    public int RegistrosInsertados { get; set;}
    public int RegistrosActualizados { get; set;}
    public int RegistrosError { get; set;} 
    public int OmitidosPorError { get; set;}
}