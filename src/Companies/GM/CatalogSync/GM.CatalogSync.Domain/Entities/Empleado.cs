namespace GM.CatalogSync.Domain.Entities;

public class Empleado
{
    public int EmpresaId {get; set;}
    public int IdEmpleado { get; set;}
    public string DealerId { get; set;} = string.Empty;
    public int Activo {get; set;}
    public string Curp {get; set;} = string.Empty;
    public string NumeroEmpleado {get; set;} = string.Empty;
    public string Nombre {get; set;} = string.Empty;
    public string PrimerApellido {get; set;} = string.Empty;
    public string SegundoApellido {get; set;} = string.Empty;
    public string Departamento { get; set;} = string.Empty;
    public string Puesto {get; set;} = string.Empty;
    public DateTime FechaNacimiento { get; set;}
    public int Edad {get; set;}
    public string EmailOrganizacional {get; set;} = string.Empty;
    public string Telefono { get; set;} = string.Empty;
    public DateTime FechaIngreso {get; set;}
    public string JefeNombre { get; set;} = string.Empty;
    public string JefePrimerApellido { get; set;} = string.Empty;
    public string JefeSegundoApellido { get; set;} = string.Empty;
    public string Antiguedad { get; set;} = string.Empty;
}