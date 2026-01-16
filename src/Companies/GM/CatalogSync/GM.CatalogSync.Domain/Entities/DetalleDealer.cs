namespace GM.CatalogSync.Domain.Entities;

public class DetalleDealer
{
    public int EmpresaId { get; set;}
    public string DealerId { get; set;} = string.Empty;
    public string Nombre {get; set;} = string.Empty;
    public string RazonSocial { get; set;} = string.Empty;
    public string Rfc { get; set;} = string.Empty;
    public int Empleados { get; set;}
    public string Tipo { get; set;} = string.Empty;
}