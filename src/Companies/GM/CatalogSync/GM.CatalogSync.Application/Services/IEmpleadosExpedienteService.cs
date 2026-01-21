using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Services;

public interface IEmpleadoExpedienteService
{
    Task<(List<EmpleadosExpedienteRespuestaDto> data, int totalRecords)> ObtenerEmpleadosExpedienteAsync(
        int? idDocumento,
        int? idEmpleado,
        int? claveTipoDocumento,
        int? empresaId,
        int page,
        int pageSize,
        string currentUser,
        string correlationId
    );
}