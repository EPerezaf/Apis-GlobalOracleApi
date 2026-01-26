using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Interfaces.Services
{
    public interface ICargaExpedienteService
    {
        Task<CargaExpedienteResponseDto> InsertarAsync(
            int empresaId,
            int empleadoId,
            string usuarioActual,
            InsertarCargaExpedienteDto dto,
            string correlationId);

        Task<CargaExpedienteResponseDto> ActualizarAsync(
            int documentoId,
            int empresaId,
            int empleadoId,
            string usuarioActual,
            ActualizarCargaExpedienteDto dto,
            string correlationId);

        Task<string> ObtenerNombreDocumentoAsync(
            int claveTipoDocumento,
            string correlationId);
    }
}