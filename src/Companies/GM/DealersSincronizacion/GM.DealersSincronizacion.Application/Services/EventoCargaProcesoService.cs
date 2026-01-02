using GM.DealersSincronizacion.Application.DTOs;
using GM.DealersSincronizacion.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GM.DealersSincronizacion.Application.Services;

/// <summary>
/// Servicio de evento de carga de proceso para dealers.
/// </summary>
public class EventoCargaProcesoService : IEventoCargaProcesoService
{
    private readonly IEventoCargaProcesoRepository _repository;
    private readonly ILogger<EventoCargaProcesoService> _logger;

    public EventoCargaProcesoService(
        IEventoCargaProcesoRepository repository,
        ILogger<EventoCargaProcesoService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EventoCargaProcesoDto?> ObtenerPorIdAsync(int id)
    {
        _logger.LogInformation("🔷 [SERVICE] Obteniendo registro de evento de carga con ID {Id}", id);

        var entidad = await _repository.ObtenerPorIdAsync(id);

        if (entidad == null)
        {
            _logger.LogWarning("⚠️ [SERVICE] Registro de evento de carga con ID {Id} no encontrado", id);
            return null;
        }

        _logger.LogInformation("✅ [SERVICE] Registro de evento de carga con ID {Id} obtenido exitosamente", id);
        return MapearADto(entidad);
    }

    /// <inheritdoc />
    public async Task<(List<EventoCargaProcesoDto> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        string? idCarga = null,
        bool? actual = null,
        int page = 1,
        int pageSize = 200)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] Obteniendo registros de evento de carga con filtros. Proceso: {Proceso}, IdCarga: {IdCarga}, Actual: {Actual}, Página: {Page}, PageSize: {PageSize}",
            proceso ?? "null", idCarga ?? "null", actual?.ToString() ?? "null", page, pageSize);

        var (entidades, totalRecords) = await _repository.ObtenerTodosConFiltrosAsync(proceso, idCarga, actual, page, pageSize);

        _logger.LogInformation(
            "✅ [SERVICE] Se obtuvieron {Cantidad} registros de evento de carga de {Total} totales (Página {Page})",
            entidades.Count, totalRecords, page);

        var dtos = entidades.Select(MapearADto).ToList();
        return (dtos, totalRecords);
    }

    /// <inheritdoc />
    public async Task<EventoCargaProcesoActualDto?> ObtenerActualAsync()
    {
        _logger.LogInformation("🔷 [SERVICE] Obteniendo registro actual de evento de carga de proceso");

        var evento = await _repository.ObtenerActualAsync();

        if (evento == null)
        {
            _logger.LogWarning("⚠️ [SERVICE] No se encontró registro actual de evento de carga de proceso");
            return null;
        }

        var dto = new EventoCargaProcesoActualDto
        {
            EventoCargaProcesoId = evento.EventoCargaProcesoId,
            Proceso = evento.Proceso,
            NombreArchivo = evento.NombreArchivo,
            FechaCarga = evento.FechaCarga,
            IdCarga = evento.IdCarga,
            Registros = evento.Registros,
            Actual = evento.Actual,
            TablaRelacion = evento.TablaRelacion
            // NOTA: DealersTotales, DealersSincronizados y PorcDealersSinc no se exponen a los dealers
        };

        _logger.LogInformation("✅ [SERVICE] Registro actual obtenido exitosamente. ID: {Id}", dto.EventoCargaProcesoId);
        return dto;
    }

    /// <inheritdoc />
    public async Task<EventoCargaProcesoActualDto?> ObtenerActualPorProcesoAsync(string proceso)
    {
        _logger.LogInformation("🔷 [SERVICE] Obteniendo registro actual de evento de carga de proceso para proceso: {Proceso}", proceso);

        var evento = await _repository.ObtenerActualPorProcesoAsync(proceso);

        if (evento == null)
        {
            _logger.LogWarning("⚠️ [SERVICE] No se encontró registro actual de evento de carga de proceso para proceso: {Proceso}", proceso);
            return null;
        }

        var dto = new EventoCargaProcesoActualDto
        {
            EventoCargaProcesoId = evento.EventoCargaProcesoId,
            Proceso = evento.Proceso,
            NombreArchivo = evento.NombreArchivo,
            FechaCarga = evento.FechaCarga,
            IdCarga = evento.IdCarga,
            Registros = evento.Registros,
            Actual = evento.Actual,
            TablaRelacion = evento.TablaRelacion
            // NOTA: DealersTotales, DealersSincronizados y PorcDealersSinc no se exponen a los dealers
        };

        _logger.LogInformation("✅ [SERVICE] Registro actual obtenido exitosamente para proceso {Proceso}. ID: {Id}", proceso, dto.EventoCargaProcesoId);
        return dto;
    }

    /// <summary>
    /// Mapea una entidad EventoCargaProceso a un DTO EventoCargaProcesoDto (sin campos de dealers).
    /// </summary>
    private EventoCargaProcesoDto MapearADto(GM.CatalogSync.Domain.Entities.EventoCargaProceso entidad)
    {
        return new EventoCargaProcesoDto
        {
            EventoCargaProcesoId = entidad.EventoCargaProcesoId,
            Proceso = entidad.Proceso,
            NombreArchivo = entidad.NombreArchivo,
            FechaCarga = entidad.FechaCarga,
            IdCarga = entidad.IdCarga,
            Registros = entidad.Registros,
            Actual = entidad.Actual,
            TablaRelacion = entidad.TablaRelacion,
            ComponenteRelacionado = entidad.ComponenteRelacionado,
            FechaAlta = entidad.FechaAlta,
            UsuarioAlta = entidad.UsuarioAlta,
            FechaModificacion = entidad.FechaModificacion,
            UsuarioModificacion = entidad.UsuarioModificacion
            // NOTA: DealersTotales, DealersSincronizados y PorcDealersSinc no se exponen a los dealers
        };
    }
}

