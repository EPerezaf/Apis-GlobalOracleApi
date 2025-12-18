using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GM.CatalogSync.Application.Services;

/// <summary>
/// Implementación del servicio para Sincronización de Archivos por Dealer.
/// </summary>
public class SincArchivoDealerService : ISincArchivoDealerService
{
    private readonly ISincArchivoDealerRepository _repository;
    private readonly ILogger<SincArchivoDealerService> _logger;

    public SincArchivoDealerService(
        ISincArchivoDealerRepository repository,
        ILogger<SincArchivoDealerService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SincArchivoDealerDto?> ObtenerPorIdAsync(int id)
    {
        _logger.LogInformation("🔷 [SERVICE] Obteniendo registro de sincronización con ID {Id}", id);

        var entidad = await _repository.ObtenerPorIdAsync(id);

        if (entidad == null)
        {
            _logger.LogWarning("⚠️ [SERVICE] Registro de sincronización con ID {Id} no encontrado", id);
            return null;
        }

        _logger.LogInformation("✅ [SERVICE] Registro de sincronización con ID {Id} obtenido exitosamente", id);
        return MapearADto(entidad);
    }

    /// <inheritdoc />
    public async Task<(List<SincArchivoDealerDto> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        string? idCarga = null,
        string? dealerBac = null,
        int page = 1,
        int pageSize = 200)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] Obteniendo registros de sincronización con filtros. Proceso: {Proceso}, IdCarga: {IdCarga}, DealerBac: {DealerBac}, Página: {Page}, PageSize: {PageSize}",
            proceso ?? "null", idCarga ?? "null", dealerBac ?? "null", page, pageSize);

        var (entidades, totalRecords) = await _repository.ObtenerTodosConFiltrosAsync(proceso, idCarga, dealerBac, page, pageSize);

        _logger.LogInformation(
            "✅ [SERVICE] Se obtuvieron {Cantidad} registros de sincronización de {Total} totales (Página {Page})",
            entidades.Count, totalRecords, page);

        var dtos = entidades.Select(MapearADto).ToList();
        return (dtos, totalRecords);
    }

    /// <inheritdoc />
    public async Task<SincArchivoDealerDto> CrearAsync(CrearSincArchivoDealerDto dto, string usuarioAlta)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] Iniciando creación de registro de sincronización. Proceso: {Proceso}, IdCarga: {IdCarga}, DealerBac: {DealerBac}, Usuario: {Usuario}",
            dto.Proceso, dto.IdCarga, dto.DealerBac, usuarioAlta);

        // Validar datos requeridos
        if (string.IsNullOrWhiteSpace(dto.Proceso))
        {
            throw new SincArchivoDealerValidacionException("El proceso es requerido");
        }

        if (string.IsNullOrWhiteSpace(dto.IdCarga))
        {
            throw new SincArchivoDealerValidacionException("El ID de carga es requerido");
        }

        if (string.IsNullOrWhiteSpace(dto.DmsOrigen))
        {
            throw new SincArchivoDealerValidacionException("El DMS origen es requerido");
        }

        if (string.IsNullOrWhiteSpace(dto.DealerBac))
        {
            throw new SincArchivoDealerValidacionException("El código BAC del dealer es requerido");
        }

        if (string.IsNullOrWhiteSpace(dto.NombreDealer))
        {
            throw new SincArchivoDealerValidacionException("El nombre del dealer es requerido");
        }

        // Validar que no exista duplicado (proceso + idCarga + dealerBac)
        var existeRegistro = await _repository.ExisteRegistroAsync(
            dto.Proceso.Trim(), 
            dto.IdCarga.Trim(), 
            dto.DealerBac.Trim());

        if (existeRegistro)
        {
            _logger.LogWarning(
                "⚠️ [SERVICE] Ya existe un registro con Proceso: '{Proceso}', IdCarga: '{IdCarga}', DealerBac: '{DealerBac}'. Usuario: {Usuario}",
                dto.Proceso, dto.IdCarga, dto.DealerBac, usuarioAlta);
            throw new SincArchivoDealerDuplicadoException(dto.Proceso, dto.IdCarga, dto.DealerBac);
        }

        // Crear entidad
        var entidad = new SincArchivoDealer
        {
            Proceso = dto.Proceso.Trim(),
            IdCarga = dto.IdCarga.Trim(),
            DmsOrigen = dto.DmsOrigen.Trim(),
            DealerBac = dto.DealerBac.Trim(),
            NombreDealer = dto.NombreDealer.Trim(),
            FechaSincronizacion = dto.FechaSincronizacion,
            RegistrosSincronizados = dto.RegistrosSincronizados
        };

        // Crear registro
        var entidadCreada = await _repository.CrearAsync(entidad, usuarioAlta);

        _logger.LogInformation(
            "✅ [SERVICE] Registro de sincronización creado exitosamente. ID: {Id}, Proceso: {Proceso}, DealerBac: {DealerBac}",
            entidadCreada.SincArchivoDealerId, entidadCreada.Proceso, entidadCreada.DealerBac);

        return MapearADto(entidadCreada);
    }

    /// <summary>
    /// Mapea una entidad a DTO.
    /// </summary>
    private static SincArchivoDealerDto MapearADto(SincArchivoDealer entidad)
    {
        return new SincArchivoDealerDto
        {
            SincArchivoDealerId = entidad.SincArchivoDealerId,
            Proceso = entidad.Proceso,
            IdCarga = entidad.IdCarga,
            DmsOrigen = entidad.DmsOrigen,
            DealerBac = entidad.DealerBac,
            NombreDealer = entidad.NombreDealer,
            FechaSincronizacion = entidad.FechaSincronizacion,
            RegistrosSincronizados = entidad.RegistrosSincronizados,
            FechaAlta = entidad.FechaAlta,
            UsuarioAlta = entidad.UsuarioAlta,
            FechaModificacion = entidad.FechaModificacion,
            UsuarioModificacion = entidad.UsuarioModificacion
        };
    }
}

