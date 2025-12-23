using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Services;

/// <summary>
/// Interfaz del servicio para Sincronización de Archivos por Dealer.
/// </summary>
public interface ISincArchivoDealerService
{
    /// <summary>
    /// Obtiene un registro por su ID.
    /// </summary>
    Task<SincArchivoDealerDto?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Obtiene todos los registros con filtros opcionales y paginación.
    /// </summary>
    /// <param name="proceso">Filtro por proceso (opcional)</param>
    /// <param name="cargaArchivoSincronizacionId">Filtro por ID de carga de archivo de sincronización (opcional)</param>
    /// <param name="dealerBac">Filtro por código BAC del dealer (opcional)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Tupla con la lista de registros y el total de registros</returns>
    Task<(List<SincArchivoDealerDto> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        int? cargaArchivoSincronizacionId = null,
        string? dealerBac = null,
        int page = 1,
        int pageSize = 200);

    /// <summary>
    /// Crea un nuevo registro de sincronización de archivos por dealer.
    /// </summary>
    /// <param name="dto">Datos del nuevo registro</param>
    /// <param name="usuarioAlta">Usuario que realiza la operación</param>
    /// <returns>Registro creado</returns>
    Task<SincArchivoDealerDto> CrearAsync(CrearSincArchivoDealerDto dto, string usuarioAlta);
}

