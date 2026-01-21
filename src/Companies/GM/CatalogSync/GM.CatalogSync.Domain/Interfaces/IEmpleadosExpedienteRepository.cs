using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

public interface IEmpleadoExpedienteRepository
{
    Task<(List<EmpleadoExpediente> empleadosExpediente, int totalRecords)> GetByFilterAsync(
        int? idDocumento,
        int? idEmpleado,
        int? claveTipoDocumento,
        int? empresaId,
        int page,
        int pageSize,
        string correlationId,
        string currentUser);
}