using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Services;

/// <summary>
/// Interfaz del servicio para Sincronización de Carga de Proceso por Dealer.
/// </summary>
public interface ISincCargaProcesoDealerService
{
    /// <summary>
    /// Obtiene un registro por su ID.
    /// </summary>
    Task<SincCargaProcesoDealerDto?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Obtiene todos los registros con filtros opcionales y paginación.
    /// </summary>
    /// <param name="proceso">Filtro por proceso (opcional)</param>
    /// <param name="eventoCargaProcesoId">Filtro por ID de evento de carga de proceso (opcional)</param>
    /// <param name="dealerBac">Filtro por código BAC del dealer (opcional)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Tupla con la lista de registros y el total de registros</returns>
    Task<(List<SincCargaProcesoDealerDto> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        int? eventoCargaProcesoId = null,
        string? dealerBac = null,
        int page = 1,
        int pageSize = 200);

    /// <summary>
    /// Crea un nuevo registro de sincronización de carga de proceso por dealer.
    /// </summary>
    /// <param name="dto">Datos del nuevo registro</param>
    /// <param name="usuarioAlta">Usuario que realiza la operación</param>
    /// <param name="dealerBac">Código BAC del dealer obtenido del JWT</param>
    /// <returns>Registro creado</returns>
    Task<SincCargaProcesoDealerDto> CrearAsync(CrearSincCargaProcesoDealerDto dto, string usuarioAlta, string dealerBac);
}

