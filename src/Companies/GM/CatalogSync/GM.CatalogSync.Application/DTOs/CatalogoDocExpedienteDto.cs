using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace GM.CatalogSync.Application.DTOs;

public class CatalogoDocResponseDto
{
    public int EmpresaId { get; set; }
    public int IdEmpleado { get; set; }
    public int ClaveTipoDocumento { get; set; }
    public string NombreTipoDocumento { get; set; } = string.Empty;
    public string Obligatorio { get; set; } = string.Empty;
    public int IdDocumento { get; set; }
    public string NombreArchivoStorage { get; set; } = string.Empty;
    public string RutaStorage { get; set; } = string.Empty;
    public string ContainerStorage { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;
    public DateTime FechaCarga { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public int EsVigente { get; set; }
    public string EstatusExpediente { get; set; } = string.Empty;
    public int ExisteArchivo { get; set; }
}