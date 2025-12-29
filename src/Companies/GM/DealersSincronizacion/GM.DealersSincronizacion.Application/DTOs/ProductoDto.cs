namespace GM.DealersSincronizacion.Application.DTOs;

/// <summary>
/// DTO para Producto (para dealers).
/// </summary>
public class ProductoDto
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
}




