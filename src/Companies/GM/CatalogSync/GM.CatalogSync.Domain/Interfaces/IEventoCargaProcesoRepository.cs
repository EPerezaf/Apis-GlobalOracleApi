using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

/// <summary>
/// Interfaz del repositorio para Evento de Carga de Proceso.
/// </summary>
public interface IEventoCargaProcesoRepository
{
    /// <summary>
    /// Obtiene un registro por su ID.
    /// </summary>
    Task<EventoCargaProceso?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Obtiene todos los registros con filtros opcionales y paginación.
    /// </summary>
    /// <param name="proceso">Filtro por proceso (opcional)</param>
    /// <param name="idCarga">Filtro por ID de carga (opcional)</param>
    /// <param name="actual">Filtro por estado actual (opcional)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Tupla con la lista de registros y el total de registros</returns>
    Task<(List<EventoCargaProceso> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        string? idCarga = null,
        bool? actual = null,
        int page = 1,
        int pageSize = 200);

    /// <summary>
    /// Verifica si existe un registro con el ID de carga especificado.
    /// </summary>
    Task<bool> ExisteIdCargaAsync(string idCarga);

    /// <summary>
    /// Crea un nuevo registro y actualiza los anteriores del mismo proceso.
    /// Ejecuta en transacción:
    /// 1. UPDATE: Todos los registros del proceso con COCP_ACTUAL = 0
    /// 2. INSERT: Nuevo registro con COCP_ACTUAL = 1
    /// </summary>
    /// <param name="entidad">Entidad a crear</param>
    /// <param name="usuarioAlta">Usuario que realiza la operación</param>
    /// <returns>Entidad creada con ID asignado</returns>
    Task<EventoCargaProceso> CrearConTransaccionAsync(
        EventoCargaProceso entidad,
        string usuarioAlta);

    /// <summary>
    /// Actualiza los contadores de dealers sincronizados y porcentaje en una transacción existente.
    /// </summary>
    /// <param name="eventoCargaProcesoId">ID del registro a actualizar</param>
    /// <param name="dealersSincronizados">Número de dealers sincronizados</param>
    /// <param name="porcDealersSinc">Porcentaje de dealers sincronizados</param>
    /// <param name="usuarioModificacion">Usuario que realiza la modificación</param>
    /// <param name="transaction">Transacción existente (debe estar activa)</param>
    /// <returns>Número de filas afectadas</returns>
    Task<int> ActualizarContadoresDealersAsync(
        int eventoCargaProcesoId,
        int dealersSincronizados,
        decimal porcDealersSinc,
        string usuarioModificacion,
        System.Data.IDbTransaction transaction);

    /// <summary>
    /// Actualiza el valor de DealersTotales basado en el conteo de dealers únicos en EventoCargaSnapshotDealer.
    /// </summary>
    /// <param name="eventoCargaProcesoId">ID del registro a actualizar</param>
    /// <param name="usuarioModificacion">Usuario que realiza la modificación</param>
    /// <returns>Número de filas afectadas</returns>
    Task<int> ActualizarDealersTotalesAsync(
        int eventoCargaProcesoId,
        string usuarioModificacion);

    /// <summary>
    /// Actualiza el valor de DealersTotales basado en el conteo de dealers únicos en EventoCargaSnapshotDealer dentro de una transacción existente.
    /// </summary>
    /// <param name="eventoCargaProcesoId">ID del registro a actualizar</param>
    /// <param name="usuarioModificacion">Usuario que realiza la modificación</param>
    /// <param name="transaction">Transacción existente (debe estar activa)</param>
    /// <returns>Número de filas afectadas</returns>
    Task<int> ActualizarDealersTotalesAsync(
        int eventoCargaProcesoId,
        string usuarioModificacion,
        System.Data.IDbTransaction transaction);
}

