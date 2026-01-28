using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Services;

    public interface ICargaExpedienteService
    {
        Task<CargaExpedienteResponseDto> CrearExpedienteAsync(
            int empresaId,
            int idEmpleado,
            string currentUser,
            CrearCargaExpedienteDto dto,
            string correlationId);

        Task<CargaExpedienteResponseDto> ActualizarExpedienteAsync(
            int documentoId,
            int empresaId,
            int idEmpleado,
            string currentUser,
            ActualizarCargaExpedienteDto dto,
            string correlationId);

        Task<CargaExpedienteResponseDto?> ObtenerPorIdAsync(
            int documentoId,
            string correlationId);
    }
