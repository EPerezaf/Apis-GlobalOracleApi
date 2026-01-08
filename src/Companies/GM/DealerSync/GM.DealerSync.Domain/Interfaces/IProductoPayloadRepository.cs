namespace GM.DealerSync.Domain.Interfaces;

/// <summary>
/// Interfaz para obtener productos desde CO_GM_LISTAPRODUCTOS para generar payload
/// </summary>
public interface IProductoPayloadRepository
{
    /// <summary>
    /// Obtiene todos los productos activos desde CO_GM_LISTAPRODUCTOS
    /// </summary>
    Task<List<ProductoPayload>> GetAllProductosAsync();
}

/// <summary>
/// Entidad para productos en el payload
/// </summary>
public class ProductoPayload
{
    public string NombreProducto { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public string NombreModelo { get; set; } = string.Empty;
    public int AnioModelo { get; set; }
    public string ModeloInteres { get; set; } = string.Empty;
    public string MarcaNegocio { get; set; } = string.Empty;
    public string NombreLocal { get; set; } = string.Empty;
    public string? DefinicionVehiculo { get; set; }
}

