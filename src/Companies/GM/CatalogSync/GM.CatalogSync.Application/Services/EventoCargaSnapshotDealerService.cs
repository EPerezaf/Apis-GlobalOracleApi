using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Security;
using ValidationError = Shared.Exceptions.ValidationError;

namespace GM.CatalogSync.Application.Services;

/// <summary>
/// Implementación del servicio para Evento de Carga Snapshot de Dealers.
/// </summary>
public class EventoCargaSnapshotDealerService : IEventoCargaSnapshotDealerService
{
    private readonly IEventoCargaSnapshotDealerRepository _repository;
    private readonly IDistribuidorRepository _distribuidorRepository;
    private readonly ILogger<EventoCargaSnapshotDealerService> _logger;

    public EventoCargaSnapshotDealerService(
        IEventoCargaSnapshotDealerRepository repository,
        IDistribuidorRepository distribuidorRepository,
        ILogger<EventoCargaSnapshotDealerService> logger)
    {
        _repository = repository;
        _distribuidorRepository = distribuidorRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EventoCargaSnapshotDealerDto?> ObtenerPorIdAsync(int id)
    {
        _logger.LogInformation("🔷 [SERVICE] Obteniendo evento carga snapshot dealer con ID {Id}", id);

        var resultado = await _repository.ObtenerPorIdCompletoAsync(id);

        if (resultado == null)
        {
            _logger.LogWarning("⚠️ [SERVICE] Evento carga snapshot dealer con ID {Id} no encontrado", id);
            return null;
        }

        _logger.LogInformation("✅ [SERVICE] Evento carga snapshot dealer con ID {Id} obtenido exitosamente", id);
        return MapearADto(resultado);
    }

    /// <inheritdoc />
    public async Task<(List<EventoCargaSnapshotDealerDto> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        int? eventoCargaProcesoId = null,
        string? dealerBac = null,
        string? dms = null,
        int? sincronizado = null,
        int page = 1,
        int pageSize = 200)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] Obteniendo eventos carga snapshot dealers con filtros. EventoCargaProcesoId: {EventoCargaProcesoId}, DealerBac: {DealerBac}, DMS: {Dms}, Sincronizado: {Sincronizado}, Página: {Page}, PageSize: {PageSize}",
            eventoCargaProcesoId?.ToString() ?? "null",
            dealerBac ?? "null",
            dms ?? "null",
            sincronizado?.ToString() ?? "null",
            page,
            pageSize);

        var (resultados, totalRecords) = await _repository.ObtenerTodosConFiltrosCompletoAsync(
            eventoCargaProcesoId,
            dealerBac,
            dms,
            sincronizado,
            page,
            pageSize);

        _logger.LogInformation(
            "✅ [SERVICE] Se obtuvieron {Cantidad} registros de {Total} totales (Página {Page})",
            resultados.Count, totalRecords, page);

        var dtos = resultados.Select(MapearADto).ToList();
        return (dtos, totalRecords);
    }

    /// <inheritdoc />
    public async Task<List<EventoCargaSnapshotDealerDto>> CrearBatchAsync(
        int eventoCargaProcesoId,
        string usuarioAlta,
        int empresaId)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] Iniciando creación batch automática de registros para EventoCargaProcesoId: {EventoCargaProcesoId}. Usuario: {Usuario}, EmpresaId: {EmpresaId}",
            eventoCargaProcesoId, usuarioAlta, empresaId);

        // 1. Validar que el EventoCargaProcesoId exista
        var existeCarga = await _repository.ExisteEventoCargaProcesoIdAsync(eventoCargaProcesoId);
        if (!existeCarga)
        {
            _logger.LogWarning("⚠️ [SERVICE] EventoCargaProcesoId {EventoCargaProcesoId} no existe. Usuario: {Usuario}",
                eventoCargaProcesoId, usuarioAlta);
            throw new BusinessValidationException(
                $"El EventoCargaProcesoId {eventoCargaProcesoId} no existe en CO_EVENTOSCARGAPROCESO.",
                new List<ValidationError> { new ValidationError { Field = "eventoCargaProcesoId", Message = "ID de evento de carga no encontrado" } });
        }

        // 2. Obtener todos los distribuidores activos para la empresa y usuario
        var distribuidores = await _distribuidorRepository.ObtenerTodosAsync(empresaId, usuarioAlta);
        if (!distribuidores.Any())
        {
            _logger.LogWarning("⚠️ [SERVICE] No se encontraron distribuidores activos para la EmpresaId: {EmpresaId}. Usuario: {Usuario}",
                empresaId, usuarioAlta);
            return new List<EventoCargaSnapshotDealerDto>(); // Retornar lista vacía si no hay distribuidores
        }

        // 3. Filtrar distribuidores que ya tienen un registro para esta carga
        var existingRecords = await _repository.ObtenerPorEventoCargaProcesoIdAsync(eventoCargaProcesoId);
        var existingDealerBacs = new HashSet<string>(existingRecords.Select(r => r.DealerBac));

        var distribuidoresParaInsertar = distribuidores
            .Where(d => !existingDealerBacs.Contains(d.DealerBac))
            .ToList();

        if (!distribuidoresParaInsertar.Any())
        {
            _logger.LogInformation("ℹ️ [SERVICE] Todos los {CantidadTotal} distribuidores ya tienen un registro para EventoCargaProcesoId: {EventoCargaProcesoId}. No se insertarán nuevos.",
                distribuidores.Count, eventoCargaProcesoId);
            return new List<EventoCargaSnapshotDealerDto>();
        }

        // 4. Mapear a entidades EventoCargaSnapshotDealer
        var fechaRegistro = DateTimeHelper.GetMexicoDateTime();
        var entidades = distribuidoresParaInsertar.Select(d => new EventoCargaSnapshotDealer
        {
            EventoCargaProcesoId = eventoCargaProcesoId,
            DealerBac = d.DealerBac ?? string.Empty,
            NombreDealer = !string.IsNullOrWhiteSpace(d.NombreDealer) 
                ? d.NombreDealer 
                : (!string.IsNullOrWhiteSpace(d.Nombre) 
                    ? d.Nombre 
                    : d.DealerBac ?? "SIN NOMBRE"), // Usar DealerBac como fallback si ambos son NULL
            RazonSocialDealer = !string.IsNullOrWhiteSpace(d.RazonSocial) 
                ? d.RazonSocial 
                : (!string.IsNullOrWhiteSpace(d.NombreDealer) 
                    ? d.NombreDealer 
                    : (!string.IsNullOrWhiteSpace(d.Nombre) 
                        ? d.Nombre 
                        : d.DealerBac ?? "SIN RAZON SOCIAL")), // Múltiples fallbacks para RazonSocial
            Dms = string.IsNullOrWhiteSpace(d.Dms) ? "GDMS" : d.Dms,
            FechaRegistro = fechaRegistro,
            FechaAlta = DateTimeHelper.GetMexicoDateTime(),
            UsuarioAlta = usuarioAlta,
            UrlWebhook = d.UrlWebhook,
            SecretKey = d.SecretKey
        }).ToList();

        // 5. Insertar en batch
        var resultados = await _repository.CrearBatchAsync(entidades, usuarioAlta);

        _logger.LogInformation("✅ [SERVICE] Creación batch automática completada. {Cantidad} registros creados para EventoCargaProcesoId: {EventoCargaProcesoId}. Usuario: {Usuario}",
            resultados.Count, eventoCargaProcesoId, usuarioAlta);

        // Para los registros creados, obtener datos completos con JOIN
        var dtos = new List<EventoCargaSnapshotDealerDto>();
        foreach (var entidad in resultados)
        {
            var resultado = await _repository.ObtenerPorIdCompletoAsync(entidad.EventoCargaSnapshotDealerId);
            if (resultado != null)
            {
                dtos.Add(MapearADto(resultado));
            }
        }

        return dtos;
    }

    /// <summary>
    /// Mapea un resultado completo del JOIN a su DTO correspondiente.
    /// </summary>
    private static EventoCargaSnapshotDealerDto MapearADto(EventoCargaSnapshotDealerMap map)
    {
        return new EventoCargaSnapshotDealerDto
        {
            EventoCargaSnapshotDealerId = map.EventoCargaSnapshotDealerId,
            EventoCargaProcesoId = map.EventoCargaProcesoId,
            IdCarga = map.IdCarga ?? string.Empty,
            ProcesoCarga = map.ProcesoCarga ?? string.Empty,
            FechaCarga = map.FechaCarga ?? DateTime.MinValue,
            FechaSincronizacion = map.FechaSincronizacion,
            TokenConfirmacion = map.TokenConfirmacion,
            // Redondear a 2 decimales para mayor precisión
            TiempoSincronizacionHoras = map.TiempoSincronizacionHoras.HasValue 
                ? Math.Round(map.TiempoSincronizacionHoras.Value, 2, MidpointRounding.AwayFromZero) 
                : null,
            DealerBac = map.DealerBac,
            NombreDealer = map.NombreDealer,
            RazonSocialDealer = map.RazonSocialDealer,
            Dms = map.Dms,
            FechaRegistro = map.FechaRegistro,
            FechaAlta = map.FechaAlta,
            UsuarioAlta = map.UsuarioAlta,
            FechaModificacion = map.FechaModificacion,
            UsuarioModificacion = map.UsuarioModificacion,
            UrlWebhook = map.UrlWebhook,
            SecretKey = map.SecretKey
        };
    }
}

