using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

/// <summary>
/// Interfaz del repositorio para Foto de Dealer Productos.
/// </summary>
public interface IFotoDealerProductosRepository
{
    /// <summary>
    /// Obtiene un registro por su ID.
    /// </summary>
    Task<FotoDealerProductos?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Obtiene todos los registros con filtros opcionales y paginación.
    /// </summary>
    /// <param name="cargaArchivoSincronizacionId">Filtro por ID de carga (opcional)</param>
    /// <param name="dealerBac">Filtro por código BAC del dealer (opcional)</param>
    /// <param name="dms">Filtro por sistema DMS (opcional)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Tupla con la lista de registros y el total de registros</returns>
    Task<(List<FotoDealerProductos> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        int? cargaArchivoSincronizacionId = null,
        string? dealerBac = null,
        string? dms = null,
        int page = 1,
        int pageSize = 200);

    /// <summary>
    /// Verifica si existe un registro con la combinación de cargaArchivoSincronizacionId y dealerBac.
    /// </summary>
    /// <param name="cargaArchivoSincronizacionId">ID de carga de archivo de sincronización</param>
    /// <param name="dealerBac">Código BAC del dealer</param>
    /// <returns>True si existe, False si no existe</returns>
    Task<bool> ExisteCombinacionAsync(int cargaArchivoSincronizacionId, string dealerBac);

    /// <summary>
    /// Crea múltiples registros en batch usando transacción.
    /// </summary>
    /// <param name="entidades">Lista de entidades a crear</param>
    /// <param name="usuarioAlta">Usuario que realiza la operación</param>
    /// <returns>Lista de entidades creadas con IDs asignados</returns>
    Task<List<FotoDealerProductos>> CrearBatchAsync(
        List<FotoDealerProductos> entidades,
        string usuarioAlta);
}

