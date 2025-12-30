using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GM.CatalogSync.Application.DTOs;

/// <summary>
/// DTO para lectura de Foto de Dealers Carga Archivos Sincronización.
/// </summary>
public class FotoDealersCargaArchivosSincDto
{
    /// <summary>
    /// Identificador único del registro.
    /// </summary>
    public int FotoDealersCargaArchivosSincId { get; set; }

    /// <summary>
    /// Identificador de la carga de archivo de sincronización (FK).
    /// </summary>
    public int CargaArchivoSincronizacionId { get; set; }

    /// <summary>
    /// ID de la carga (desde CO_CARGAARCHIVOSINCRONIZACION).
    /// </summary>
    public string IdCarga { get; set; } = string.Empty;

    /// <summary>
    /// Proceso de la carga (desde CO_CARGAARCHIVOSINCRONIZACION).
    /// </summary>
    public string ProcesoCarga { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de carga (desde CO_CARGAARCHIVOSINCRONIZACION).
    /// </summary>
    public DateTime FechaCarga { get; set; }

    /// <summary>
    /// Fecha de sincronización (desde CO_SINCRONIZACIONARCHIVOSDEALERS, puede ser null si no existe registro).
    /// </summary>
    public DateTime? FechaSincronizacion { get; set; }

    /// <summary>
    /// Token de confirmación (desde CO_SINCRONIZACIONARCHIVOSDEALERS, puede ser null si no existe registro).
    /// </summary>
    public string? TokenConfirmacion { get; set; }

    /// <summary>
    /// Tiempo de sincronización en horas (diferencia entre FechaSincronizacion y FechaCarga).
    /// Si FechaSincronizacion es null, este valor será null.
    /// </summary>
    public decimal? TiempoSincronizacionHoras { get; set; }

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
/// DTO para creación de Foto de Dealers Carga Archivos Sincronización.
/// NOTA: Este DTO ya no se usa directamente en el request. Los distribuidores se generan automáticamente desde CO_DISTRIBUIDORES.
/// Se mantiene para compatibilidad interna del servicio.
/// </summary>
[Obsolete("Este DTO ya no se usa en el request. Los distribuidores se generan automáticamente desde CO_DISTRIBUIDORES.")]
public class CrearFotoDealersCargaArchivosSincDto
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

    // NOTA: FechaRegistro se calcula automáticamente en el servicio (no se envía en el request)
}

/// <summary>
/// DTO para carga batch de Fotos de Dealers Carga Archivos Sincronización.
/// Los distribuidores se generan automáticamente desde CO_DISTRIBUIDORES basándose en el empresaId del JWT.
/// </summary>
public class CrearFotoDealersCargaArchivosSincBatchDto
{
    /// <summary>
    /// Identificador de la carga de archivo de sincronización (FK).
    /// Los distribuidores se obtendrán automáticamente desde CO_DISTRIBUIDORES filtrados por empresaId del JWT.
    /// </summary>
    [Required(ErrorMessage = "El ID de carga de archivo de sincronización es requerido")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID de carga debe ser mayor a 0")]
    public int CargaArchivoSincronizacionId { get; set; }

    // NOTA: Los siguientes campos se obtienen automáticamente desde CO_DISTRIBUIDORES:
    // - dealerBac: DEALERID
    // - nombreDealer: CODI_NOMBRE
    // - razonSocialDealer: CODI_RAZONSOCIAL
    // - dms: CODI_DMS (con default "GDMS" si está vacío)
    // - fechaRegistro: Se calcula automáticamente con hora de México
}

