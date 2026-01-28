using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces
{
    public interface ICargaExpedienteRepository
    {
        Task<int> InsertarAsync(
            CargaExpediente expediente,
            string currentUser,
            string correlationId);
            
        Task<bool> ActualizarAsync(
            CargaExpediente expediente,
            string currentUser,
            string correlationId);
            
        Task<CargaExpediente?> ObtenerPorIdAsync(
            int idDocumento,
            string correlationId);
    }
}