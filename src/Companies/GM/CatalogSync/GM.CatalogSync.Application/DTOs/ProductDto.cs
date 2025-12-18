using System.ComponentModel.DataAnnotations;

namespace GM.CatalogSync.Application.DTOs
{
    /// <summary>
    /// DTO para creación de productos (POST)
    /// </summary>
    public class ProductCreateDto
    {
        [Required]
        public string NombreProducto { get; set; } = string.Empty;

        [Required]
        public string Pais { get; set; } = string.Empty;

        [Required]
        public string NombreModelo { get; set; } = string.Empty;

        [Required]
        [Range(1900, 2100)]
        public int AnioModelo { get; set; }

        [Required]
        public string ModeloInteres { get; set; } = string.Empty;

        [Required]
        public string MarcaNegocio { get; set; } = string.Empty;

        public string? NombreLocal { get; set; }
        public string? DefinicionVehiculo { get; set; }
    }

    /// <summary>
    /// DTO para respuesta de productos (GET)
    /// </summary>
    public class ProductResponseDto
    {
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public string Pais { get; set; } = string.Empty;
        public string NombreModelo { get; set; } = string.Empty;
        public int AnioModelo { get; set; }
        public string ModeloInteres { get; set; } = string.Empty;
        public string MarcaNegocio { get; set; } = string.Empty;
        public string? NombreLocal { get; set; }
        public string? DefinicionVehiculo { get; set; }
        public DateTime? FechaAlta { get; set; }
        public string? UsuarioAlta { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public string? UsuarioModificacion { get; set; }
    }

    /// <summary>
    /// DTO para request batch (POST)
    /// </summary>
    public class ProductBatchRequestDto
    {
        [Required]
        public List<ProductCreateDto> Json { get; set; } = new();
    }

    /// <summary>
    /// DTO para resultado batch
    /// </summary>
    public class ProductBatchResultDto
    {
        public int RegistrosTotales { get; set; }
        public int RegistrosInsertados { get; set; }
        public int RegistrosActualizados { get; set; }
        public int RegistrosError { get; set; }
        public int OmitidosPorError { get; set; }
    }
}

