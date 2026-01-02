namespace GM.CatalogSync.Domain.Entities;

/// <summary>
/// Entidad que representa un registro de evento de carga de proceso.
/// Tabla: CO_EVENTOSCARGAPROCESO
/// </summary>
public class EventoCargaProceso
{
    /// <summary>
    /// Identificador único del registro de evento (PK).
    /// Columna: COCP_EVENTOCARGAPROCESOID
    /// </summary>
    public int EventoCargaProcesoId { get; set; }

    /// <summary>
    /// Nombre del proceso de sincronización (ej: ProductsCatalog, InventorySync).
    /// Columna: COCP_PROCESO
    /// </summary>
    public string Proceso { get; set; } = string.Empty;

    /// <summary>
    /// Nombre completo del archivo cargado.
    /// Columna: COCP_NOMBREARCHIVO
    /// </summary>
    public string NombreArchivo { get; set; } = string.Empty;

    /// <summary>
    /// Fecha y hora exacta en que se realizó la carga del archivo.
    /// Columna: COCP_FECHACARGA
    /// </summary>
    public DateTime FechaCarga { get; set; }

    /// <summary>
    /// Identificador único de la carga (formato: proceso_fecha_hora).
    /// Columna: COCP_IDCARGA - UNIQUE
    /// </summary>
    public string IdCarga { get; set; } = string.Empty;

    /// <summary>
    /// Número total de registros procesados en la carga.
    /// Columna: COCP_REGISTROS
    /// </summary>
    public int Registros { get; set; }

    /// <summary>
    /// Indicador si es la carga actual: true=actual, false=no actual.
    /// Columna: COCP_ACTUAL (1=true, 0=false)
    /// </summary>
    public bool Actual { get; set; }

    /// <summary>
    /// Número total de dealers a sincronizar.
    /// Columna: COCP_DEALERSTOTALES
    /// </summary>
    public int DealersTotales { get; set; }

    /// <summary>
    /// Número de dealers sincronizados.
    /// Columna: COCP_DEALERSSINCRONIZADOS
    /// </summary>
    public int? DealersSincronizados { get; set; }

    /// <summary>
    /// Porcentaje de dealers sincronizados.
    /// Columna: COCP_PORCDEALERSSINC (NUMBER(5,2))
    /// </summary>
    public decimal? PorcDealersSinc { get; set; }

    /// <summary>
    /// Nombre de la tabla relacionada.
    /// Columna: COCP_TABLARELACION
    /// </summary>
    public string? TablaRelacion { get; set; }

    /// <summary>
    /// Componente relacionado.
    /// Columna: COCP_COMPONENTERELACIONADO
    /// </summary>
    public string? ComponenteRelacionado { get; set; }

    // ========================================
    // CAMPOS DE AUDITORÍA
    // ========================================

    /// <summary>
    /// Fecha de creación del registro.
    /// Columna: COCP_FECHAALTA
    /// </summary>
    public DateTime FechaAlta { get; set; }

    /// <summary>
    /// Usuario que realizó la carga del archivo.
    /// Columna: COCP_USUARIOALTA
    /// </summary>
    public string UsuarioAlta { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de última modificación del registro.
    /// Columna: COCP_FECHAMODIFICACION
    /// </summary>
    public DateTime? FechaModificacion { get; set; }

    /// <summary>
    /// Usuario que realizó la última modificación.
    /// Columna: COCP_USUARIOMODIFICACION
    /// </summary>
    public string? UsuarioModificacion { get; set; }
}

