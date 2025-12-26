namespace GM.DealersSincronizacion.Domain.Entities;

/// <summary>
/// Entidad de dominio Distribuidor (Dealer).
/// Representa un distribuidor/concesionario en la tabla CO_DISTRIBUIDORES.
/// </summary>
public class Distribuidor
{
    public int DealerId { get; set; }
    public string DealerBac { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? Rfc { get; set; }
    public string? Zona { get; set; }
    public string? SiteCode { get; set; }
    public string Dms { get; set; } = string.Empty;
    public string? NombreDealer { get; set; }
    public string? Marca { get; set; }
}

