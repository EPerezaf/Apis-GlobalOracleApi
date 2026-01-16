using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Services;

public interface IEmpleadoService
{
    Task<(List<EmpleadoRespuestaDto> data, int totalRecords)> ObtenerEmpleadosAsync(
        int? idEmpleado,
        int? dealerId,
        string? curp,
        string? numeroEmpleado,
        int page,
        int pageSize,
        string currentUser,
        string correlationId);

    /*Task<int> EliminarTodosAsync(
        string currentUser,
        string correlationId);*/
    
    
}