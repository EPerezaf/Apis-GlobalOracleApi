namespace GM.CatalogSync.Domain.Entities;

/// <summary>
/// Entidad que representa un registro de carga de archivo para sincronización.
/// Tabla: CO_CARGAARCHIVOSINCRONIZACION
/// </summary>
public class CargaArchivoSincronizacion
{
    /// <summary>
    /// Identificador único del registro de carga (PK).
    /// Columna: COCA_CARGAARCHIVOSINID
    /// </summary>
    public int CargaArchivoSincronizacionId { get; set; }

    /// <summary>
    /// Nombre del proceso de sincronización (ej: ProductsCatalog, InventorySync).
    /// Columna: COCA_PROCESO
    /// </summary>
    public string Proceso { get; set; } = string.Empty;

    /// <summary>
    /// Nombre completo del archivo cargado.
    /// Columna: COCA_NOMBREARCHIVO
    /// </summary>
    public string NombreArchivo { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora exacta en que se realizó la carga del archivo.
    /// Columna: COCA_FECHACARGA
    /// </summary>
    public DateTime FechaCarga { get; set; }

    /// <summary>
    /// Identificador único de la carga (formato: proceso_fecha_hora).
    /// Columna: COCA_IDCARGA - UNIQUE
    /// </summary>
    public string IdCarga { get; set; } = string.Empty;

    /// <summary>
    /// Número total de registros procesados en la carga.
    /// Columna: COCA_REGISTROS
    /// </summary>
    public int Registros { get; set; }

    /// <summary>
    /// Indicador si es la carga actual: true=actual, false=no actual.
    /// Columna: COCA_ACTUAL (1=true, 0=false)
    /// </summary>
    public bool Actual { get; set; }

    /// <summary>
    /// Número total de dealers a sincronizar.
    /// Columna: COCA_DEALERSTOTALES (NOT NULL, default 0)
    /// </summary>
    public int DealersTotales { get; set; }

    /// <summary>
    /// Número de dealers sincronizados.
    /// Columna: COCA_DEALERSSONCRONIZADOS (nullable, default 0)
    /// </summary>
    public int? DealersSincronizados { get; set; }

    /// <summary>
    /// Porcentaje de dealers sincronizados.
    /// Columna: COCA_PORCDEALERSSINC (NUMBER(5,2), nullable, default 0.00)
    /// </summary>
    public decimal? PorcDealersSinc { get; set; }

    /// <summary>
    /// Nombre de la tabla relacionada.
    /// Columna: COCA_TABLARELACION
    /// </summary>
    public string? TablaRelacion { get; set; }

    // ========================================
    // CAMPOS DE AUDITORÍA
    // ========================================

    /// <summary>
    /// Fecha de creación del registro.
    /// Columna: FECHAALTA
    /// </summary>
    public DateTime FechaAlta { get; set; }

    /// <summary>
    /// Usuario que realizó la carga del archivo.
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

