namespace GM.CatalogSync.Domain.Entities;

public class DetalleDealer
{
    public int EmpresaId { get; set;}
    public string DealerId { get; set;} = string.Empty;
    public string Nombre {get; set;} = string.Empty;
    public string RazonSocial { get; set;} = string.Empty;
    public string Rfc { get; set;} = string.Empty;
    public int Activo { get; set;}
    public int Empleados { get; set;}
    public string Tipo { get; set;} = string.Empty;
    public string Marcas { get; set;} = string.Empty;
    public int Distrito { get; set;} 
    public string Dms { get; set;} = string.Empty;
    public string ClienteId {get; set;} = string.Empty;
    public string ClienteSecreto { get;set;} = string.Empty;
    
}