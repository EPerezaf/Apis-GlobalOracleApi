using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace GM.CatalogSync.Application.DTOs;

public class DetalleCrearDto
{
    [Required]
    public string DealerId { get; set;} = string.Empty;
    [Required]
    public string Nombre {get; set;} = string.Empty;
    [Required]
    public string RazonSocial { get; set;} = string.Empty;
    [Required]
    public string Zona { get; set;} = string.Empty;
    [Required]
    public string Rfc { get; set;} = string.Empty;
    [Required]
    public string Marca { get; set;} = string.Empty;
    [Required]
    public int NoDealer {get; set;}
    [Required]
    public int SiteCode {get; set;}
    [Required]
    public string Tipo { get; set;} = string.Empty;
    [Required]
    public string Marcas { get; set;} = string.Empty;
    [Required]
    public int Distrito { get; set;} 
    [Required]
    public int EmpresaId { get; set;}
    public string Dms { get; set;} = string.Empty;
    public string ClienteId {get; set;} = string.Empty;
    public string ClienteSecreto { get;set;} = string.Empty;

}

public class DetalleDealerRespuestaDto
{
    public int EmpresaId { get; set;}
    public string DealerId { get; set;} = string.Empty;
    public string Nombre {get; set;} = string.Empty;
    public string RazonSocial { get; set;} = string.Empty;
    public string Rfc { get; set;} = string.Empty;
    public int Empleados { get; set;}
    public string Tipo { get; set;} = string.Empty;
}