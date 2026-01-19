using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Services;

public interface IDetalleService
{
    Task<(List<DetalleDealerRespuestaDto> data, int totalRecords)> ObtenerDelearAsync(
        string? dealerId,
        string? nombre,
        string? razonSocial,
        string? rfc,
<<<<<<< HEAD
        int? noDealer,
=======
        int? empresaId,
>>>>>>> 49a386a (Se agrego el login y filtrado por empresa)
        int page,
        int pageSize,
        string currentUser,
        string correlationId);
}