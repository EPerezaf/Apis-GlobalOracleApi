using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Services;

public interface ICatalogoDocExpedienteService
{
    Task<(List<CatalogoDocResponseDto> data, int totalRecords)> ObtenerCatalogoAsync(
        int? claveTipoDocumento,
        int? idEmpleado,
        int? empresaId,
        int? idDocumento,
        string? estatusExpediente,
        int page,
        int pageSize,
        string currentUser,
        string correlationId);
}