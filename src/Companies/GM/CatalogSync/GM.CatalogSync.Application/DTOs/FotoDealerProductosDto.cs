using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GM.CatalogSync.Application.DTOs;

/// <summary>
/// DTO para lectura de Foto de Dealer Productos.
/// </summary>
public class FotoDealerProductosDto
{
    /// <summary>
    /// Identificador único del registro.
    /// </summary>
    public int FotoDealerProductosId { get; set; }

    /// <summary>
    /// Identificador de la carga de archivo de sincronización (FK).
    /// </summary>
    public int CargaArchivoSincronizacionId { get; set; }

    /// <summary>
    /// Código BAC del dealer (FK).
    /// </summary>
    public string DealerBac { get; set; } = string.Empty;

    /// <summary>
    /// Nombre comercial del dealer.
    /// </summary>
    public string NombreDealer { get; set; } = string.Empty;

    /// <summary>
    /// Razón social legal del dealer.
    /// </summary>
    public string RazonSocialDealer { get; set; } = string.Empty;

    /// <summary>
    /// Sistema DMS utilizado.
    /// </summary>
    public string Dms { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de registro de la fotografía.
    /// </summary>
    public DateTime FechaRegistro { get; set; }

    /// <summary>
    /// Fecha de alta del registro.
    /// </summary>
    public DateTime FechaAlta { get; set; }

    /// <summary>
    /// Usuario que realizó el alta.
    /// </summary>
    public string UsuarioAlta { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de última modificación.
    /// </summary>
    public DateTime? FechaModificacion { get; set; }

    /// <summary>
    /// Usuario que realizó la última modificación.
    /// </summary>
    public string? UsuarioModificacion { get; set; }
}

/// <summary>
/// DTO para creación de Foto de Dealer Productos (para batch insert).
/// </summary>
public class CrearFotoDealerProductosDto
{
    /// <summary>
    /// Identificador de la carga de archivo de sincronización (FK).
    /// </summary>
    [Required(ErrorMessage = "El ID de carga de archivo de sincronización es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID de carga debe ser mayor a 0")]
    public int CargaArchivoSincronizacionId { get; set; }

    /// <summary>
    /// Código BAC del dealer (FK).
    /// </summary>
    [Required(ErrorMessage = "El código BAC del dealer es requerido")]
    [StringLength(100, ErrorMessage = "El código BAC no puede exceder 100 caracteres")]
    public string DealerBac { get; set; } = string.Empty;

    /// <summary>
    /// Nombre comercial del dealer.
    /// </summary>
    [Required(ErrorMessage = "El nombre del dealer es requerido")]
    [StringLength(400, ErrorMessage = "El nombre del dealer no puede exceder 400 caracteres")]
    public string NombreDealer { get; set; } = string.Empty;

    /// <summary>
    /// Razón social legal del dealer.
    /// </summary>
    [Required(ErrorMessage = "La razón social del dealer es requerida")]
    [StringLength(400, ErrorMessage = "La razón social no puede exceder 400 caracteres")]
    public string RazonSocialDealer { get; set; } = string.Empty;

    /// <summary>
    /// Sistema DMS utilizado.
    /// </summary>
    [Required(ErrorMessage = "El sistema DMS es requerido")]
    [StringLength(400, ErrorMessage = "El sistema DMS no puede exceder 400 caracteres")]
    public string Dms { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de registro de la fotografía.
    /// </summary>
    [Required(ErrorMessage = "La fecha de registro es requerida")]
    public DateTime FechaRegistro { get; set; }
}

/// <summary>
/// DTO para carga batch de Fotos de Dealer Productos.
/// </summary>
public class CrearFotoDealerProductosBatchDto
{
    /// <summary>
    /// Lista de registros a crear (formato JSON).
    /// </summary>
    [Required(ErrorMessage = "El campo 'json' es requerido")]
    [MinLength(1, ErrorMessage = "Debe incluir al menos un registro en 'json'")]
    [JsonPropertyName("json")]
    public List<CrearFotoDealerProductosDto> Json { get; set; } = new();
}

