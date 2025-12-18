namespace GM.CatalogSync.Domain.Entities
{
    /// <summary>
    /// Entidad de dominio Product
    /// </summary>
    public class Product
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
}

