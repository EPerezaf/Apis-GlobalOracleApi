using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces
{
    public interface ICatalogoDocExpedienteRepository
    {
        Task<(List<CatalogoDocExpediente> catalogo, int totalRecords)> GetByFilterAsync(
            int? claveTipoDocumento,
            int? idEmpleado,
            int? empresaId,
            int? idDocumento,
            string? estatusExpediente,
            string currentUser,
            string correlationId,
            int page,
            int pageSize);
    }
}