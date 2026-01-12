using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Services;

public interface ICampañaService
{
    Task<(List<CampañaRespuestDto> data, int totalRecords)> ObtenerCampañasAsync(
        string? SourceCodeId,
        string? id,
        string? name,
        int page,
        int pageSize,
        string currentUser,
        string correlationId);
    

    Task<CampañaBatchResultadoDto> ProcesarBatchInsertAsync( 
        List<CampañaCrearDto> campañas,
        string currentUser, 
        string correlationId);

    Task<int> EliminarTodosAsync( 
        string currentUser,
        string correlationId);
}