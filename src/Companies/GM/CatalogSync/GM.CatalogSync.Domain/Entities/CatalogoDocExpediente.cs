using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace GM.CatalogSync.Domain.Entities
{
    public class CatalogoDocExpediente
    {
        public int EmpresaId { get; set; }
        public int IdEmpleado { get; set; }
        public int ClaveTipoDocumento { get; set; }
        public string NombreTipoDocumento { get; set; } = string.Empty;
        public string Obligatorio { get; set; } = string.Empty;
        public int IdDocumento { get; set; }
        public string NombreArchivoStorage { get; set; } = string.Empty;
        public string ContainerStorage { get; set; } = string.Empty;
        public string RutaStorage { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public DateTime FechaCarga { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public int EsVigente { get; set; }
        public string EstatusExpediente { get; set; } = string.Empty;
        public int ExisteArchivo { get; set; }

    }
}