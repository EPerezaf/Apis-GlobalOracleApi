namespace GM.CatalogSync.Domain.Entities;

public class DetalleDealer
{
    public string DealerId { get; set;} = string.Empty;
    public string Nombre {get; set;} = string.Empty;
    public string RazonSocial { get; set;} = string.Empty;
    public string Zona { get; set;} = string.Empty;
    public string Rfc { get; set;} = string.Empty;
    public string Marca { get; set;} = string.Empty;
    public int NoDealer {get; set;}
    public int SiteCode {get; set;}
    public string Tipo { get; set;} = string.Empty;
    public string Marcas { get; set;} = string.Empty;
    public int Distrito { get; set;} 
    public int EmpresaId { get; set;}
    public string Dms { get; set;} = string.Empty;
    public string ClienteId {get; set;} = string.Empty;
    public string ClienteSecreto { get;set;} = string.Empty;
    
}