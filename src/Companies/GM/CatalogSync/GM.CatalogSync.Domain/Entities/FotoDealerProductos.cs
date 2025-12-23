namespace GM.CatalogSync.Domain.Entities;

/// <summary>
/// Entidad que representa un registro de foto de dealer productos.
/// Tabla: CO_FOTODEALERPRODUCTOS
/// </summary>
public class FotoDealerProductos
{
    /// <summary>
    /// Identificador único del registro (PK).
    /// Columna: COFD_FOTODEALERPRODUCTOSID
    /// </summary>
    public int FotoDealerProductosId { get; set; }

    /// <summary>
    /// Identificador de la carga de archivo de sincronización (FK).
    /// Columna: COFD_COCA_CARGAARCHIVOSINID
    /// </summary>
    public int CargaArchivoSincronizacionId { get; set; }

    /// <summary>
    /// Código BAC del dealer (FK).
    /// Columna: COSA_DEALERBAC
    /// </summary>
    public string DealerBac { get; set; } = string.Empty;

    /// <summary>
    /// Nombre comercial del dealer.
    /// Columna: COFD_NOMBREDEALER
    /// </summary>
    public string NombreDealer { get; set; } = string.Empty;

    /// <summary>
    /// Razón social legal del dealer.
    /// Columna: COFD_RAZONSOCIALDEALER
    /// </summary>
    public string RazonSocialDealer { get; set; } = string.Empty;

    /// <summary>
    /// Sistema DMS utilizado.
    /// Columna: COFD_DMS
    /// </summary>
    public string Dms { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de registro de la fotografía.
    /// Columna: COFD_FECHAREGISTRO
    /// </summary>
    public DateTime FechaRegistro { get; set; }

    // ========================================
    // CAMPOS DE AUDITORÍA
    // ========================================

    /// <summary>
    /// Fecha de creación del registro.
    /// Columna: FECHAALTA
    /// </summary>
    public DateTime FechaAlta { get; set; }

    /// <summary>
    /// Usuario que creó el registro.
    /// Columna: USUARIOALTA
    /// </summary>
    public string UsuarioAlta { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de última modificación del registro.
    /// Columna: FECHAMODIFICACION
    /// </summary>
    public DateTime? FechaModificacion { get; set; }

    /// <summary>
    /// Usuario que realizó la última modificación.
    /// Columna: USUARIOMODIFICACION
    /// </summary>
    public string? UsuarioModificacion { get; set; }
}

