namespace GM.CatalogSync.Domain.Entities;

public class EmpleadoExpediente
{
    public int EmpresaId { get; set;}
    public int IdDocumento { get; set;}
    public int IdEmpleado { get; set;}
    public string NumeroEmpleado { get; set;} = string.Empty;
    public string Nombre { get;set;} = string.Empty;
    public string PrimerApellido { get;set;} = string.Empty;
    public string SegundoApellido { get;set;} = string.Empty;
    public int ClaveTipoDocumento { get; set;}
    public string NombreTipoDocumento { get; set;} = string.Empty;
    public string NombreDocumento { get;set;} = string.Empty;
    public string NombreArchivoStorage { get; set;} = string.Empty;
        public string RutaStorage { get;set;} = string.Empty;

    public string Descripcion { get; set;} = string.Empty;
    public string Obligatorio { get; set; } = string.Empty;
    public string RequiereVigencia { get; set;} = string.Empty;
    public string ContainerStorage { get;set;} = string.Empty;
    public int VersionDocumento { get; set;}
    public int EsVigente { get;set;}
    public int Orden { get;set;}
    public DateTime FechaCarga { get;set;}
    public DateTime FechaDocumento { get;set;}
    public DateTime FechaVencimiento {get;set;}
    public string Observaciones { get;set;} = string.Empty;

    /*public int IdDocumento {get;set;}
    public int IdEmpleado { get;set;}
    public string ClaveTipoDocumento {get;set;} = string.Empty;
    public string NombreDocumento {get;set;} =string.Empty;
    public string NombreArchivoStorage {get; set;} = string.Empty;
    public string RutaStorage {get;set;} = string.Empty;
    public string ContainerStorage { get;set;} = string.Empty;
    public int VersionDocumento {get;set;}
    public int EsVigente {get;set;} 
    public DateTime FechaCarga { get;set;}
    public DateTime FechaDocumento { get; set;}
    public DateTime FechaVencimiento { get;set;}
    public string Observaciones { get; set; } = string.Empty;
    public string UsuarioAlta { get; set;} = string.Empty;
    public DateTime FechaAlta { get;set;} 
    public string UsuarioModificacion { get; set;} = string.Empty;
    public DateTime FechaModificacion { get;set;}*/
}