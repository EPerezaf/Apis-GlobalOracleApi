using System.ComponentModel.DataAnnotations;

namespace GM.CatalogSync.Application.DTOs;

public class EmpleadoCrearDto
{
    [Required]
    public string IdEmpleado { get; set;} = string.Empty;
    [Required]
    public string EmpresaId {get; set;} = string.Empty;
    [Required]
    public string UsuarioId {get; set;} = string.Empty;
    [Required]
    public int Activo {get;set;}
    [Required]
    public string Curp { get;set;} = string.Empty;
    [Required]
    public string Rfc { get; set;} = string.Empty;
    [Required]
    public string NumeroEmpleado { get; set;} = string.Empty;
    [Required]
    public string Nombre { get; set;} = string.Empty;
    [Required]
    public string PrimerApellido { get; set;} = string.Empty;
    [Required]
    public string SegundoApellido { get; set;} = string.Empty;
    [Required]
    public DateTime FechaNacimiento {get; set;}
    [Required]
    public DateTime FechaIngreso {get;set;}
    [Required]
    public string EmailOrganizacional {get; set;} = string.Empty;
}

public class EmpleadoRespuestaDto
{
    public int EmpresaId {get; set;}
    public int IdEmpleado { get; set;}
    public string DealerId { get; set;} = string.Empty;
    public string Activo {get; set;} = string.Empty;
    public string Curp {get; set;} = string.Empty;
    public string NumeroEmpleado {get; set;} = string.Empty;
    public string NombreCompleto {get; set;} = string.Empty;
    public string Departamento { get; set;} = string.Empty;
    public string Puesto {get; set;} = string.Empty;
    public DateTime FechaNacimiento { get; set;}
    public int Edad {get; set;}
    public string EmailOrganizacional {get; set;} = string.Empty;
    public string Telefono { get; set;} = string.Empty;
    public DateTime FechaIngreso {get; set;}
    public string JefeInmediato { get; set;} = string.Empty;
}