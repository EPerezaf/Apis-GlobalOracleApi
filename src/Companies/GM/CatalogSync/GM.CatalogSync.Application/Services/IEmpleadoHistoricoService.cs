using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Services;

public interface IEmpleadoHistoricoService
{
    Task <(List<EmpleadoHistoricoRespuestaDto> data, int totalRecords)>ObtenerEmpleadosHistoricosAsync(
        int? idAsignacion,
        int? idEmpleado,
        int? dealerId,
        string? clavePuesto,
        string? departamento,
        int? esActual,
        int? empresaId,
        int page,
        int pageSize,
        string currentUser,
        string correlationId);
}