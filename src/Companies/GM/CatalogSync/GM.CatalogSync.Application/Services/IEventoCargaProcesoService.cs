using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Services;

/// <summary>
/// Interfaz del servicio para Evento de Carga de Proceso.
/// </summary>
public interface IEventoCargaProcesoService
{
    /// <summary>
    /// Obtiene un registro por su ID.
    /// </summary>
    Task<EventoCargaProcesoDto?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Obtiene todos los registros con filtros opcionales y paginación.
    /// </summary>
    /// <param name="proceso">Filtro por proceso (opcional)</param>
    /// <param name="idCarga">Filtro por ID de carga (opcional)</param>
    /// <param name="actual">Filtro por estado actual (opcional)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Tupla con la lista de registros y el total de registros</returns>
    Task<(List<EventoCargaProcesoDto> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        string? idCarga = null,
        bool? actual = null,
        int page = 1,
        int pageSize = 200);

    /// <summary>
    /// Crea un nuevo registro de evento de carga de proceso.
    /// Automáticamente marca los registros anteriores del mismo proceso como no actuales.
    /// </summary>
    /// <param name="dto">Datos del nuevo registro</param>
    /// <param name="usuarioAlta">Usuario que realiza la operación</param>
    /// <returns>Registro creado</returns>
    Task<EventoCargaProcesoDto> CrearAsync(
        CrearEventoCargaProcesoDto dto,
        string usuarioAlta);

    /// <summary>
    /// Actualiza el valor de DealersTotales basado en el conteo de dealers únicos en EventoCargaSnapshotDealer.
    /// </summary>
    /// <param name="eventoCargaProcesoId">ID del registro a actualizar</param>
    /// <param name="usuarioModificacion">Usuario que realiza la modificación</param>
    /// <returns>Registro actualizado</returns>
    Task<EventoCargaProcesoDto> ActualizarDealersTotalesAsync(
        int eventoCargaProcesoId,
        string usuarioModificacion);
}
