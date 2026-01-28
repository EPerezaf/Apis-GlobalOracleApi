namespace GM.CatalogSync.Domain.Entities
{
    public class CargaExpediente
    {
        public int EmpresaId { get; set;}
        public int IdDocumento { get; set;}
        public int IdEmpleado { get; set;}
        public int ClaveTipoDocumento { get; set;}
        public string NombreDocumento { get;set;} =string.Empty;
        public string NombreArchivoStorage {get; set;} = string.Empty;
        public string RutaStorage {get;set;} = string.Empty;
        public string ContainerStorage { get;set;} = string.Empty;
        public int VersionDocumento {get;set;}
        public int EsVigente {get;set;}
        public DateTime FechaCarga { get;set;}
        public DateTime FechaDocumento { get; set;}
        public DateTime? FechaVencimiento { get;set;}
        public string? Observaciones { get; set; } = string.Empty;
        public string UsuarioAlta { get; set;} = string.Empty;
        public DateTime FechaAlta { get;set;}
        public string UsuarioModificacion { get; set;} = string.Empty;
        public DateTime FechaModificacion { get;set;}
    }
}