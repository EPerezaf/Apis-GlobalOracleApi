using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

/// <summary>
/// Interfaz del repositorio para Sincronización de Archivos por Dealer.
/// </summary>
public interface ISincArchivoDealerRepository
{
    /// <summary>
    /// Obtiene un registro por su ID.
    /// </summary>
    Task<SincArchivoDealer?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Obtiene todos los registros con filtros opcionales y paginación.
    /// </summary>
    /// <param name="proceso">Filtro por proceso (opcional)</param>
    /// <param name="idCarga">Filtro por ID de carga (opcional)</param>
    /// <param name="dealerBac">Filtro por código BAC del dealer (opcional)</param>
    /// <param name="page">Número de página (por defecto: 1)</param>
    /// <param name="pageSize">Tamaño de página (por defecto: 200)</param>
    /// <returns>Tupla con la lista de registros y el total de registros</returns>
    Task<(List<SincArchivoDealer> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        string? idCarga = null,
        string? dealerBac = null,
        int page = 1,
        int pageSize = 200);

    /// <summary>
    /// Verifica si existe un registro con la combinación proceso, idCarga y dealerBac.
    /// </summary>
    Task<bool> ExisteRegistroAsync(string proceso, string idCarga, string dealerBac);

    /// <summary>
    /// Crea un nuevo registro de sincronización.
    /// </summary>
    /// <param name="entidad">Entidad a crear</param>
    /// <param name="usuarioAlta">Usuario que realiza la operación</param>
    /// <returns>Entidad creada con ID asignado</returns>
    Task<SincArchivoDealer> CrearAsync(SincArchivoDealer entidad, string usuarioAlta);
}

