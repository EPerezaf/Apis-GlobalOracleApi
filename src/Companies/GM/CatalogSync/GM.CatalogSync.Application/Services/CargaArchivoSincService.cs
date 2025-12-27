using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Security;

namespace GM.CatalogSync.Application.Services;

/// <summary>
/// Implementación del servicio para Carga de Archivo de Sincronización.
/// </summary>
public class CargaArchivoSincService : ICargaArchivoSincService
{
    private readonly ICargaArchivoSincRepository _repository;
    private readonly ILogger<CargaArchivoSincService> _logger;

    public CargaArchivoSincService(
        ICargaArchivoSincRepository repository,
        ILogger<CargaArchivoSincService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CargaArchivoSincDto?> ObtenerPorIdAsync(int id)
    {
        _logger.LogInformation("🔷 [SERVICE] Obteniendo registro de carga con ID {Id}", id);

        var entidad = await _repository.ObtenerPorIdAsync(id);

        if (entidad == null)
        {
            _logger.LogWarning("⚠️ [SERVICE] Registro de carga con ID {Id} no encontrado", id);
            return null;
        }

        _logger.LogInformation("✅ [SERVICE] Registro de carga con ID {Id} obtenido exitosamente", id);
        return MapearADto(entidad);
    }

    /// <inheritdoc />
    public async Task<(List<CargaArchivoSincDto> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        string? idCarga = null,
        bool? actual = null,
        int page = 1,
        int pageSize = 200)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] Obteniendo registros de carga con filtros. Proceso: {Proceso}, IdCarga: {IdCarga}, Actual: {Actual}, Página: {Page}, PageSize: {PageSize}",
            proceso ?? "null", idCarga ?? "null", actual?.ToString() ?? "null", page, pageSize);

        var (entidades, totalRecords) = await _repository.ObtenerTodosConFiltrosAsync(proceso, idCarga, actual, page, pageSize);

        _logger.LogInformation(
            "✅ [SERVICE] Se obtuvieron {Cantidad} registros de carga de {Total} totales (Página {Page})",
            entidades.Count, totalRecords, page);

        var dtos = entidades.Select(MapearADto).ToList();
        return (dtos, totalRecords);
    }

    /// <inheritdoc />
    public async Task<CargaArchivoSincDto> CrearAsync(
        CrearCargaArchivoSincDto dto,
        string usuarioAlta)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] Iniciando creación de registro de carga. Proceso: {Proceso}, IdCarga: {IdCarga}, Usuario: {Usuario}",
            dto.Proceso, dto.IdCarga, usuarioAlta);

        // Validar que el IdCarga no exista
        var existeIdCarga = await _repository.ExisteIdCargaAsync(dto.IdCarga);
        if (existeIdCarga)
        {
            _logger.LogWarning(
                "⚠️ [SERVICE] El ID de carga '{IdCarga}' ya existe. Usuario: {Usuario}",
                dto.IdCarga, usuarioAlta);
            throw new IdCargaDuplicadoException(dto.IdCarga);
        }

        // Validar datos requeridos
        if (string.IsNullOrWhiteSpace(dto.Proceso))
        {
            throw new CargaArchivoSincValidacionException("El proceso es requerido");
        }

        if (string.IsNullOrWhiteSpace(dto.NombreArchivo))
        {
            throw new CargaArchivoSincValidacionException("El nombre del archivo es requerido");
        }

        if (string.IsNullOrWhiteSpace(dto.IdCarga))
        {
            throw new CargaArchivoSincValidacionException("El ID de carga es requerido");
        }

        // Crear entidad
        var entidad = new CargaArchivoSincronizacion
        {
            Proceso = dto.Proceso.Trim(),
            NombreArchivo = dto.NombreArchivo.Trim(),
            FechaCarga = DateTimeHelper.GetMexicoDateTime(), // Calculado automáticamente (hora de México)
            IdCarga = dto.IdCarga.Trim(),
            Registros = dto.Registros,
            DealersTotales = dto.DealersTotales,
            TablaRelacion = !string.IsNullOrWhiteSpace(dto.TablaRelacion) ? dto.TablaRelacion.Trim() : null,
            DealersSincronizados = 0, // Default 0
            PorcDealersSinc = 0.00m, // Default 0.00
            Actual = true // Siempre se crea como actual
        };

        // Crear con transacción (actualiza anteriores y crea nuevo)
        var entidadCreada = await _repository.CrearConTransaccionAsync(entidad, usuarioAlta);

        _logger.LogInformation(
            "✅ [SERVICE] Registro de carga creado exitosamente. ID: {Id}, Proceso: {Proceso}, IdCarga: {IdCarga}",
            entidadCreada.CargaArchivoSincronizacionId, entidadCreada.Proceso, entidadCreada.IdCarga);

        return MapearADto(entidadCreada);
    }

    /// <inheritdoc />
    public async Task<CargaArchivoSincDto> ActualizarDealersTotalesAsync(
        int cargaArchivoSincronizacionId,
        string usuarioModificacion)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] Actualizando DealersTotales. CargaArchivoSincronizacionId: {Id}, Usuario: {Usuario}",
            cargaArchivoSincronizacionId, usuarioModificacion);

        // Verificar que existe el registro
        var entidad = await _repository.ObtenerPorIdAsync(cargaArchivoSincronizacionId);
        if (entidad == null)
        {
            _logger.LogWarning(
                "⚠️ [SERVICE] No se encontró registro de carga con ID {Id}",
                cargaArchivoSincronizacionId);
            throw new NotFoundException(
                $"No se encontró un registro de carga con ID {cargaArchivoSincronizacionId}",
                "CargaArchivoSincronizacion",
                cargaArchivoSincronizacionId.ToString());
        }

        // Actualizar DealersTotales (cuenta dealers únicos en FotoDealersCargaArchivosSinc)
        var filasAfectadas = await _repository.ActualizarDealersTotalesAsync(
            cargaArchivoSincronizacionId,
            usuarioModificacion);

        if (filasAfectadas == 0)
        {
            _logger.LogWarning(
                "⚠️ [SERVICE] No se actualizó ningún registro. ID: {Id}",
                cargaArchivoSincronizacionId);
            throw new NotFoundException(
                $"No se encontró un registro de carga con ID {cargaArchivoSincronizacionId}",
                "CargaArchivoSincronizacion",
                cargaArchivoSincronizacionId.ToString());
        }

        // Obtener el registro actualizado
        var entidadActualizada = await _repository.ObtenerPorIdAsync(cargaArchivoSincronizacionId);
        if (entidadActualizada == null)
        {
            _logger.LogError(
                "❌ [SERVICE] Error al obtener registro actualizado. ID: {Id}",
                cargaArchivoSincronizacionId);
            throw new BusinessException("Error al obtener el registro actualizado");
        }

        _logger.LogInformation(
            "✅ [SERVICE] DealersTotales actualizado exitosamente. ID: {Id}, DealersTotales: {DealersTotales}",
            cargaArchivoSincronizacionId, entidadActualizada.DealersTotales);

        return MapearADto(entidadActualizada);
    }

    /// <summary>
    /// Mapea una entidad a DTO.
    /// </summary>
    private static CargaArchivoSincDto MapearADto(CargaArchivoSincronizacion entidad)
    {
        return new CargaArchivoSincDto
        {
            CargaArchivoSincId = entidad.CargaArchivoSincronizacionId,
            Proceso = entidad.Proceso,
            NombreArchivo = entidad.NombreArchivo,
            FechaCarga = entidad.FechaCarga,
            IdCarga = entidad.IdCarga,
            Registros = entidad.Registros,
            Actual = entidad.Actual,
            DealersTotales = entidad.DealersTotales,
            DealersSincronizados = entidad.DealersSincronizados,
            PorcDealersSinc = entidad.PorcDealersSinc,
            TablaRelacion = entidad.TablaRelacion,
            FechaAlta = entidad.FechaAlta,
            UsuarioAlta = entidad.UsuarioAlta,
            FechaModificacion = entidad.FechaModificacion,
            UsuarioModificacion = entidad.UsuarioModificacion
        };
    }
}

