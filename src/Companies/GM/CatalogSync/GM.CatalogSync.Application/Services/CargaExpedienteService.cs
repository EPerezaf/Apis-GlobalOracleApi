using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Interfaces.Services;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GM.CatalogSync.Application.Services
{
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

        public async Task<CargaExpedienteResponseDto> InsertarAsync(
            int empresaId,
            int empleadoId,
            string usuarioActual,
            InsertarCargaExpedienteDto dto,
            string correlationId)
        {
            _logger.LogInformation(
                "🔷 [SERVICE] CorrelationId {CorrelationId} - Iniciando creación de expediente. EmpresaId: {EmpresaId}, EmpleadoId: {EmpleadoId}",
                correlationId,
                empresaId,
                empleadoId);

            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // Validaciones
            if (string.IsNullOrWhiteSpace(dto.NombreArchivoStorage))
                throw new ArgumentException("El nombre del archivo es requerido", nameof(dto.NombreArchivoStorage));

            if (string.IsNullOrWhiteSpace(dto.RutaStorage))
                throw new ArgumentException("La ruta del archivo es requerida", nameof(dto.RutaStorage));

            if (string.IsNullOrWhiteSpace(dto.ContainerStorage))
                throw new ArgumentException("El container es requerido", nameof(dto.ContainerStorage));

            // Obtener nombre del documento desde catálogo
            var nombreDocumento = await ObtenerNombreDocumentoAsync(
                dto.ClaveTipoDocumento,
                correlationId);

            // Mapear DTO → Entity
            var entidad = new CargaExpediente
            {
                EmpresaId = empresaId,
                IdEmpleado = empleadoId,
                ClaveTipoDocumento = dto.ClaveTipoDocumento,
                NombreDocumento = nombreDocumento,
                NombreArchivoStorage = dto.NombreArchivoStorage,
                RutaStorage = dto.RutaStorage,
                ContainerStorage = dto.ContainerStorage,
                VersionDocumento = 1, // Primera versión siempre
                EsVigente = 1, // Activo por defecto
                FechaDocumento = dto.FechaDocumento,
                FechaVencimiento = dto.FechaVencimiento ?? DateTime.MaxValue,
                Observaciones = dto.Observaciones ?? string.Empty,
                UsuarioAlta = usuarioActual
            };

            var idDocumento = await _repository.InsertarAsync(entidad, correlationId);

            _logger.LogInformation(
                "✅ [SERVICE] CorrelationId {CorrelationId} - Expediente creado exitosamente con ID {IdDocumento}",
                correlationId,
                idDocumento);

            return new CargaExpedienteResponseDto
            {
                IdDocumento = idDocumento,
                Mensaje = "Expediente creado exitosamente",
                VersionDocumento = 1
            };
        }

        public async Task<CargaExpedienteResponseDto> ActualizarAsync(
            int documentoId,
            int empresaId,
            int empleadoId,
            string usuarioActual,
            ActualizarCargaExpedienteDto dto,
            string correlationId)
        {
            _logger.LogInformation(
                "🔷 [SERVICE] CorrelationId {CorrelationId} - Iniciando actualización de expediente {DocumentoId}",
                correlationId,
                documentoId);

            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (documentoId != dto.IdDocumento)
                throw new ArgumentException(
                    "El ID del documento no coincide con el ID del DTO",
                    nameof(documentoId));

            // Obtener documento actual para verificar existencia y versión
            var documentoActual = await _repository.ObtenerPorIdAsync(documentoId, correlationId);
            
            if (documentoActual == null)
                throw new InvalidOperationException($"No se encontró el documento con ID {documentoId}");

            // Determinar la nueva versión
            int nuevaVersion = documentoActual.VersionDocumento;
            
            // Si se actualizó el archivo (cambió la ruta), incrementar versión
            if (!string.IsNullOrWhiteSpace(dto.RutaStorage) && 
                dto.RutaStorage != documentoActual.RutaStorage)
            {
                nuevaVersion++;
                _logger.LogInformation(
                    "CorrelationId {CorrelationId} - Archivo actualizado, incrementando versión de {VersionAnterior} a {VersionNueva}",
                    correlationId,
                    documentoActual.VersionDocumento,
                    nuevaVersion);
            }

            // Si viene versión en el DTO, usarla (para control manual)
            if (dto.VersionDocumento.HasValue)
            {
                nuevaVersion = dto.VersionDocumento.Value;
            }

            var entidad = new CargaExpediente
            {
                IdDocumento = documentoId,
                EmpresaId = empresaId,
                IdEmpleado = empleadoId,
                // Si no vienen estos campos, mantener los actuales
                NombreArchivoStorage = dto.NombreArchivoStorage ?? documentoActual.NombreArchivoStorage,
                RutaStorage = dto.RutaStorage ?? documentoActual.RutaStorage,
                ContainerStorage = dto.ContainerStorage ?? documentoActual.ContainerStorage,
                VersionDocumento = nuevaVersion,
                FechaDocumento = dto.FechaDocumento,
                FechaVencimiento = dto.FechaVencimiento ?? DateTime.MaxValue,
                Observaciones = dto.Observaciones ?? documentoActual.Observaciones,
                UsuarioModificacion = usuarioActual
            };

            var actualizado = await _repository.ActualizarAsync(entidad, correlationId);

            if (!actualizado)
                throw new InvalidOperationException($"No se pudo actualizar el documento {documentoId}");

            _logger.LogInformation(
                "✅ [SERVICE] CorrelationId {CorrelationId} - Expediente {DocumentoId} actualizado exitosamente",
                correlationId,
                documentoId);

            return new CargaExpedienteResponseDto
            {
                IdDocumento = documentoId,
                Mensaje = "Expediente actualizado exitosamente",
                VersionDocumento = nuevaVersion
            };
        }

        public async Task<string> ObtenerNombreDocumentoAsync(
            int claveTipoDocumento,
            string correlationId)
        {
            _logger.LogInformation(
                "🔷 [SERVICE] CorrelationId {CorrelationId} - Obteniendo nombre de documento para clave {ClaveTipoDocumento}",
                correlationId,
                claveTipoDocumento);

            // TODO: Implementar consulta a catálogo
            // SELECT NOMBRE FROM LABGDMS.CO_EMPLEADOS_TIPOS_DOCUMENTOS 
            // WHERE CLAVE = :claveTipoDocumento

            var nombreDocumento = await Task.FromResult($"Documento_{claveTipoDocumento}");

            _logger.LogInformation(
                "✅ [SERVICE] CorrelationId {CorrelationId} - Nombre de documento obtenido: {NombreDocumento}",
                correlationId,
                nombreDocumento);

            return nombreDocumento;
        }
    }
}