namespace GM.DealersSincronizacion.Application.DTOs;

/// <summary>
/// DTO para respuesta de productos con información de carga de archivo de sincronización.
/// </summary>
public class ProductosConCargaDto
{
    /// <summary>
    /// Lista de productos activos.
    /// </summary>
    public List<ProductoDto> Productos { get; set; } = new();

    /// <summary>
    /// ID del evento de carga de proceso actual.
    /// </summary>
    public int EventoCargaProcesoId { get; set; }

    /// <summary>
    /// Nombre del proceso de sincronización.
    /// </summary>
    public string Proceso { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de carga del archivo.
    /// </summary>
    public DateTime FechaCarga { get; set; }

    /// <summary>
    /// ID único de la carga.
    /// </summary>
    public string IdCarga { get; set; } = string.Empty;

    /// <summary>
    /// Número de registros procesados.
    /// </summary>
    public int Registros { get; set; }

    /// <summary>
    /// Indica si es la carga actual (siempre true en esta respuesta).
    /// </summary>
    public bool Actual { get; set; }

    /// <summary>
    /// Nombre de la tabla relacionada con esta carga de sincronización.
    /// </summary>
    public string? TablaRelacion { get; set; }
}

