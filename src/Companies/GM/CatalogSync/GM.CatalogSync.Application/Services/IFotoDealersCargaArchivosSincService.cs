using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Services;

/// <summary>
/// Interfaz del servicio para Foto de Dealers Carga Archivos Sincronización.
/// </summary>
public interface IFotoDealersCargaArchivosSincService
{
    /// <summary>
    /// Obtiene un registro por su ID.
    /// </summary>
    Task<FotoDealersCargaArchivosSincDto?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Obtiene todos los registros con filtros opcionales y paginación.
    /// </summary>
    /// <param name="cargaArchivoSincronizacionId">Filtro por ID de carga (opcional)</param>
    /// <param name="dealerBac">Filtro por código BAC del dealer (opcional)</param>
    /// <param name="dms">Filtro por sistema DMS (opcional)</param>
    /// <param name="sincronizado">Filtro por estado de sincronización: null=todos, 0=no sincronizados, 1=sincronizados (opcional)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Tupla con la lista de registros y el total de registros</returns>
    Task<(List<FotoDealersCargaArchivosSincDto> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        int? cargaArchivoSincronizacionId = null,
        string? dealerBac = null,
        string? dms = null,
        int? sincronizado = null,
        int page = 1,
        int pageSize = 200);

    /// <summary>
    /// Crea múltiples registros en batch.
    /// </summary>
    /// <param name="dto">DTO con la lista de registros a crear</param>
    /// <param name="usuarioAlta">Usuario que realiza la operación</param>
    /// <returns>Lista de registros creados</returns>
    Task<List<FotoDealersCargaArchivosSincDto>> CrearBatchAsync(
        CrearFotoDealersCargaArchivosSincBatchDto dto,
        string usuarioAlta);
}

