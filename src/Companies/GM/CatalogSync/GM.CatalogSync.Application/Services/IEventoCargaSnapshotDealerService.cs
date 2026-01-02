using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Services;

/// <summary>
/// Interfaz del servicio para Evento de Carga Snapshot de Dealers.
/// </summary>
public interface IEventoCargaSnapshotDealerService
{
    /// <summary>
    /// Obtiene un registro por su ID.
    /// </summary>
    Task<EventoCargaSnapshotDealerDto?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Obtiene todos los registros con filtros opcionales y paginación.
    /// </summary>
    /// <param name="eventoCargaProcesoId">Filtro por ID de evento de carga (opcional)</param>
    /// <param name="dealerBac">Filtro por código BAC del dealer (opcional)</param>
    /// <param name="dms">Filtro por sistema DMS (opcional)</param>
    /// <param name="sincronizado">Filtro por estado de sincronización: null=todos, 0=no sincronizados, 1=sincronizados (opcional)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Tupla con la lista de registros y el total de registros</returns>
    Task<(List<EventoCargaSnapshotDealerDto> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        int? eventoCargaProcesoId = null,
        string? dealerBac = null,
        string? dms = null,
        int? sincronizado = null,
        int page = 1,
        int pageSize = 200);

    /// <summary>
    /// Crea múltiples registros en batch generando automáticamente los distribuidores desde CO_DISTRIBUIDORES.
    /// </summary>
    /// <param name="eventoCargaProcesoId">ID del evento de carga de proceso (los distribuidores se generan automáticamente)</param>
    /// <param name="usuarioAlta">Usuario que realiza la operación</param>
    /// <param name="empresaId">ID de la empresa del usuario autenticado (para filtrar distribuidores)</param>
    /// <returns>Lista de registros creados</returns>
    Task<List<EventoCargaSnapshotDealerDto>> CrearBatchAsync(
        int eventoCargaProcesoId,
        string usuarioAlta,
        int empresaId);
}

