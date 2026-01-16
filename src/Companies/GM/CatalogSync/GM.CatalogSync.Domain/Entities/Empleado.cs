namespace GM.CatalogSync.Domain.Entities;

public class Empleado
{
    public int EmpresaId {get; set;}
    public int IdEmpleado { get; set;}
    public string DealerId { get; set;} = string.Empty;
    public string Activo {get; set;} = string.Empty;
    public string Curp {get; set;} = string.Empty;
    public string NumeroEmpleado {get; set;} = string.Empty;
    public string NombreCompleto {get; set;} = string.Empty;
    public string Departamento { get; set;} = string.Empty;
    public string NombrePuesto {get; set;} = string.Empty;
    public DateTime FechaNacimiento { get; set;}
    public int Edad {get; set;}
    public string EmailOrganizacional {get; set;} = string.Empty;
    public string Telefono { get; set;} = string.Empty;
    public DateTime FechaIngreso {get; set;}
    public string JefeInmediato { get; set;} = string.Empty;
    public string? UsuarioAlta {get; set;}
    public DateTime FechaAlta {get; set;}
    public string? UsuarioModifica {get; set;}
    public DateTime FechaModifica {get;set;}
}