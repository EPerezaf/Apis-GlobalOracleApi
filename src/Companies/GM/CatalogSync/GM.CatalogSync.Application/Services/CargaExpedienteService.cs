using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace GM.CatalogSync.Application.Services;

public class CargaExpedienteService : ICargaExpedienteService
{
    private readonly ICargaExpedienteRepository _repository;
    private readonly ILogger<CargaExpedienteService> _logger;

    public CargaExpedienteService(
        ICargaExpedienteRepository repository,
        ILogger<CargaExpedienteService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CargaExpedienteResponseDto> CrearExpedienteAsync(
        int empresaId,
        int idEmpleado,
        string currentUser,
        CrearCargaExpedienteDto dto,
        string correlationId)
    {

        _logger.LogInformation(
            "🔷 [SERVICE] CorrelationId {CorrelationId} - Iniciando creación de expediente. EmpresaId: {EmpresaId}, IdEmpleado: {IdEmpleado}",
            correlationId, empresaId, idEmpleado);

        if (dto == null) throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.NombreArchivoStorage))
            throw new ArgumentException("El nombre del archivo storage es requerido.");

        // Mapear DTO a Entity 
        var entidad = new CargaExpediente
        {
            EmpresaId = empresaId,
            IdEmpleado = idEmpleado,
            ClaveTipoDocumento = dto.ClaveTipoDocumento,
            NombreDocumento = dto.NombreDocumento,
            NombreArchivoStorage = dto.NombreArchivoStorage,
            RutaStorage = dto.RutaStorage,
            ContainerStorage = dto.ContainerStorage,
            VersionDocumento = 1, // Por ser Insertar, iniciamos en 1
            EsVigente = 1,        // Activo por defecto
            FechaCarga = DateTime.Now,
            FechaDocumento = dto.FechaDocumento,
            FechaVencimiento = dto.FechaVencimiento,
            Observaciones = dto.Observaciones ?? string.Empty,
            UsuarioAlta = currentUser
        };

        // El repositorio debe devolver el ID generado
        var idDocumento = await _repository.InsertarAsync(entidad, correlationId, currentUser);

        _logger.LogInformation(
            "✅ [SERVICE] CorrelationId {CorrelationId} - Expediente creado con ID {IdDocumento}",
            correlationId, idDocumento);

        return new CargaExpedienteResponseDto
        {
        IdDocumento = entidad.IdDocumento,
        NombreDocumento = entidad.NombreDocumento,
        NombreArchivoStorage = entidad.NombreArchivoStorage,
        RutaStorage = entidad.RutaStorage,
        ContainerStorage = entidad.ContainerStorage,
        VersionDocumento = entidad.VersionDocumento,
        FechaDocumento = entidad.FechaDocumento,
        FechaVencimiento = entidad.FechaVencimiento,
        Observaciones = entidad.Observaciones,
    };
       
    }

    public async Task<CargaExpedienteResponseDto> ActualizarExpedienteAsync(
        int documentoId,
        int empresaId,
        int idEmpleado,
        string currentUser,
        ActualizarCargaExpedienteDto dto,
        string correlationId)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] CorrelationId {CorrelationId} - Actualizando expediente {DocumentoId}",
            correlationId, documentoId);

        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var documentoActual = await _repository.ObtenerPorIdAsync(documentoId, correlationId);
        if (documentoActual == null)
            throw new InvalidOperationException($"No se encontró el documento con ID {documentoId}");

        // Lógica de versionado simple
        int nuevaVersion = documentoActual.VersionDocumento;
        if (!string.IsNullOrWhiteSpace(dto.RutaStorage) && dto.RutaStorage != documentoActual.RutaStorage)
        {
            nuevaVersion++;
        }

        var entidad = new CargaExpediente
        {
            IdDocumento = documentoId,
            EmpresaId = empresaId,
            IdEmpleado = idEmpleado,
            ClaveTipoDocumento = documentoActual.ClaveTipoDocumento,
            NombreDocumento = documentoActual.NombreDocumento,
            NombreArchivoStorage = dto.NombreArchivoStorage ?? documentoActual.NombreArchivoStorage,
            RutaStorage = dto.RutaStorage ?? documentoActual.RutaStorage,
            ContainerStorage = dto.ContainerStorage ?? documentoActual.ContainerStorage,
            VersionDocumento = dto.VersionDocumento ?? nuevaVersion,
            EsVigente = 1,
            FechaDocumento = dto.FechaDocumento,
            FechaVencimiento = dto.FechaVencimiento,
            Observaciones = dto.Observaciones ?? documentoActual.Observaciones,
            UsuarioModificacion = currentUser
        };

        var actualizado = await _repository.ActualizarAsync(entidad, correlationId, currentUser);

        return new CargaExpedienteResponseDto
        {
        IdDocumento = entidad.IdDocumento,
        NombreDocumento = entidad.NombreDocumento,
        NombreArchivoStorage = entidad.NombreArchivoStorage,
        RutaStorage = entidad.RutaStorage,
        ContainerStorage = entidad.ContainerStorage,
        VersionDocumento = entidad.VersionDocumento,
        FechaDocumento = entidad.FechaDocumento,
        FechaVencimiento = entidad.FechaVencimiento,
        Observaciones = entidad.Observaciones,
    };
    }

    public async Task<CargaExpedienteResponseDto?> ObtenerPorIdAsync(
        int documentoId,
        string correlationId)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] CorrelationId {CorrelationId} - Buscando expediente {DocumentoId}",
            correlationId, documentoId);

        var expediente = await _repository.ObtenerPorIdAsync(documentoId, correlationId);

        if (expediente == null) return null;

        return new CargaExpedienteResponseDto
        {
            IdDocumento = (int)expediente.IdDocumento,
            NombreDocumento = expediente.NombreDocumento,
            NombreArchivoStorage = expediente.NombreArchivoStorage,
            RutaStorage = expediente.RutaStorage,
            ContainerStorage = expediente.ContainerStorage,
            VersionDocumento = (int)expediente.VersionDocumento,
            FechaDocumento = expediente.FechaDocumento,
            FechaVencimiento = expediente.FechaVencimiento,
            Observaciones = expediente.Observaciones ?? string.Empty
        };
    }
}
