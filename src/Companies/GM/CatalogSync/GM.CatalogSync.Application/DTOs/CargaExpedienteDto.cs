using System.ComponentModel.DataAnnotations;

namespace GM.CatalogSync.Application.DTOs
{
    public class CrearCargaExpedienteDto
    {
        [Required(ErrorMessage = "El tipo de documento es requerido")]
        public int ClaveTipoDocumento { get; set; }

        [Required(ErrorMessage = "El nombre del documento es requerido")]
        [StringLength(200)]
        public string NombreDocumento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre del archivo es requerido")]
        [StringLength(200)]
        public string NombreArchivoStorage { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ruta del archivo es requerida")]
        [StringLength(500)]
        public string RutaStorage { get; set; } = string.Empty;

        [Required(ErrorMessage = "El container es requerido")]
        [StringLength(100)]
        public string ContainerStorage { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha del documento es requerida")]
        public DateTime FechaDocumento { get; set; }

        public DateTime? FechaVencimiento { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }
    }

    public class ActualizarCargaExpedienteDto
    {
        [Required]
        public int IdDocumento { get; set; }

        // Estos campos son opcionales porque tal vez solo quieren actualizar fechas/observaciones
        [StringLength(200)]
        public string? NombreArchivoStorage { get; set; }

        [StringLength(500)]
        public string? RutaStorage { get; set; }

        [StringLength(100)]
        public string? ContainerStorage { get; set; }

        public int? VersionDocumento { get; set; }

        [Required]
        public DateTime FechaDocumento { get; set; }

        public DateTime? FechaVencimiento { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }
    }

    public class CargaExpedienteResponseDto
    {
        public int IdDocumento { get; set; }
        public string NombreDocumento { get; set; } = string.Empty;
        public string NombreArchivoStorage { get; set; } = string.Empty;
        public string RutaStorage { get; set; } = string.Empty;
        public string ContainerStorage { get; set; } = string.Empty;
        public int VersionDocumento { get; set; }
        public DateTime FechaDocumento { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public string Observaciones { get; set; } = string.Empty;
    }
}