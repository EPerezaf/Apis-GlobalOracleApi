namespace GM.CatalogSync.Domain.Entities;

public class Asignacion
{
    public int AsignacionId { get; set;}
    public string Usuario { get; set;} = string.Empty;
    public string Dealer { get; set;} = string.Empty;
    public DateTime? FechaAlta { get; set;}
    public string? UsuarioAlta { get; set;}
    public DateTime? FechaModificacion {get; set;}
    public string? UsuarioModificacion { get; set;}
    public int EmpresaId { get; set;}

}