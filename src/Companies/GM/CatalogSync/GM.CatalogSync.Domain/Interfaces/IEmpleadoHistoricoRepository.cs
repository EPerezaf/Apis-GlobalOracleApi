using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

public interface IEmpleadoHistoricoRepository
{
    Task<(List<EmpleadoHistorico> empleadosHistorico, int totalRecords)> GetByFilterAsync(
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
        string correlationId
    );
}