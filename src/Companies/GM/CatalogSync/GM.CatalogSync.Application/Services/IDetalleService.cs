using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Services;

public interface IDetalleService
{
    Task<(List<DetalleDealerRespuestaDto> data, int totalRecords)> ObtenerDelearAsync(
        string? dealerId,
        string? nombre,
        string? razonSocial,
        string? rfc,
        int? empresaId,
        int page,
        int pageSize,
        string currentUser,
        string correlationId);
}