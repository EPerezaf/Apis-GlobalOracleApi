using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Services;

public interface IAsignacionService
{
    Task<(List<AsignacionRespuestaDto> data, int totalRecords)> ObtenerAsignacionesAsync(
        string? usuario, 
        //string? dealer,
        int page, 
        int pageSize,
        string currentUser,
        string correlationId);

    Task<AsignacionBatchResultadoDto> ProcesarBatchInsertAsync(
        List<AsignacionCrearDto> asignacionDealer,
        string currentUser,
        string correlationId);
    
    Task<int> EliminarTodosAsync(
        string usuario,
        string dealer,
        string currentUser,
        string correlationId);
}