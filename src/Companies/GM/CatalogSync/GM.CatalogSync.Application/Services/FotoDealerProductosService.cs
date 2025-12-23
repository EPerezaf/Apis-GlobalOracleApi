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
/// Implementación del servicio para Foto de Dealer Productos.
/// </summary>
public class FotoDealerProductosService : IFotoDealerProductosService
{
    private readonly IFotoDealerProductosRepository _repository;
    private readonly ILogger<FotoDealerProductosService> _logger;

    public FotoDealerProductosService(
        IFotoDealerProductosRepository repository,
        ILogger<FotoDealerProductosService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FotoDealerProductosDto?> ObtenerPorIdAsync(int id)
    {
        _logger.LogInformation("🔷 [SERVICE] Obteniendo foto dealer productos con ID {Id}", id);

        var resultado = await _repository.ObtenerPorIdCompletoAsync(id);

        if (resultado == null)
        {
            _logger.LogWarning("⚠️ [SERVICE] Foto dealer productos con ID {Id} no encontrado", id);
            return null;
        }

        _logger.LogInformation("✅ [SERVICE] Foto dealer productos con ID {Id} obtenido exitosamente", id);
        return MapearADto(resultado);
    }

    /// <inheritdoc />
    public async Task<(List<FotoDealerProductosDto> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        int? cargaArchivoSincronizacionId = null,
        string? dealerBac = null,
        string? dms = null,
        int? sincronizado = null,
        int page = 1,
        int pageSize = 200)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] Obteniendo fotos dealer productos con filtros. CargaArchivoSincId: {CargaId}, DealerBac: {DealerBac}, DMS: {Dms}, Sincronizado: {Sincronizado}, Página: {Page}, PageSize: {PageSize}",
            cargaArchivoSincronizacionId?.ToString() ?? "null",
            dealerBac ?? "null",
            dms ?? "null",
            sincronizado?.ToString() ?? "null",
            page,
            pageSize);

        var (resultados, totalRecords) = await _repository.ObtenerTodosConFiltrosCompletoAsync(
            cargaArchivoSincronizacionId,
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
    public async Task<List<FotoDealerProductosDto>> CrearBatchAsync(
        CrearFotoDealerProductosBatchDto dto,
        string usuarioAlta)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] Iniciando creación batch de {Cantidad} registros. Usuario: {Usuario}",
            dto.Json.Count, usuarioAlta);

        if (dto.Json == null || !dto.Json.Any())
        {
            _logger.LogWarning("⚠️ [SERVICE] La lista de registros está vacía. Usuario: {Usuario}", usuarioAlta);
            throw new BusinessValidationException("El campo 'json' no puede estar vacío", new List<ValidationError>());
        }

        // Mapear DTOs a entidades
        var fechaRegistro = DateTimeHelper.GetMexicoDateTime(); // FechaRegistro se calcula automáticamente
        var entidades = dto.Json.Select(dtoItem => new FotoDealerProductos
        {
            CargaArchivoSincronizacionId = dtoItem.CargaArchivoSincronizacionId,
            DealerBac = dtoItem.DealerBac,
            NombreDealer = dtoItem.NombreDealer,
            RazonSocialDealer = dtoItem.RazonSocialDealer,
            Dms = dtoItem.Dms,
            FechaRegistro = fechaRegistro, // Calculado automáticamente (hora de México)
            FechaAlta = DateTimeHelper.GetMexicoDateTime(),
            UsuarioAlta = usuarioAlta
        }).ToList();

        // VALIDACIÓN PREVIA: Verificar duplicados dentro del mismo batch
        var duplicadosEnBatch = entidades
            .GroupBy(e => new { e.CargaArchivoSincronizacionId, e.DealerBac })
            .Where(g => g.Count() > 1)
            .Select(g => $"CargaArchivoSincId={g.Key.CargaArchivoSincronizacionId}, DealerBac={g.Key.DealerBac} (aparece {g.Count()} veces)")
            .ToList();

        if (duplicadosEnBatch.Any())
        {
            var mensaje = $"Se encontraron duplicados dentro del mismo batch: {string.Join("; ", duplicadosEnBatch)}";
            _logger.LogWarning("⚠️ [SERVICE] {Mensaje}. Usuario: {Usuario}", mensaje, usuarioAlta);
            throw new BusinessValidationException(mensaje, new List<ValidationError>());
        }

        // VALIDACIÓN PREVIA: Verificar que los CargaArchivoSincronizacionId existan
        var errores = new List<ValidationError>();
        var cargaArchivoSincIdsUnicos = entidades.Select(e => e.CargaArchivoSincronizacionId).Distinct().ToList();
        
        foreach (var cargaArchivoSincId in cargaArchivoSincIdsUnicos)
        {
            var existeCarga = await _repository.ExisteCargaArchivoSincronizacionIdAsync(cargaArchivoSincId);
            if (!existeCarga)
            {
                var indices = entidades
                    .Select((e, idx) => new { e, idx })
                    .Where(x => x.e.CargaArchivoSincronizacionId == cargaArchivoSincId)
                    .Select(x => x.idx)
                    .ToList();

                foreach (var indice in indices)
                {
                    errores.Add(new ValidationError
                    {
                        Field = $"json[{indice}].cargaArchivoSincronizacionId",
                        Message = $"No existe un registro de carga de archivo de sincronización con ID {cargaArchivoSincId}",
                        AttemptedValue = cargaArchivoSincId
                    });
                }
            }
        }

        // VALIDACIÓN PREVIA: Verificar duplicados en la base de datos (ANTES de insertar)
        for (int i = 0; i < entidades.Count; i++)
        {
            var entidad = entidades[i];
            var existe = await _repository.ExisteCombinacionAsync(
                entidad.CargaArchivoSincronizacionId,
                entidad.DealerBac);

            if (existe)
            {
                errores.Add(new ValidationError
                {
                    Field = $"json[{i}].(cargaArchivoSincronizacionId, dealerBac)",
                    Message = $"Ya existe un registro con CargaArchivoSincId={entidad.CargaArchivoSincronizacionId} y DealerBac={entidad.DealerBac}",
                    AttemptedValue = new { entidad.CargaArchivoSincronizacionId, entidad.DealerBac }
                });
            }
        }

        if (errores.Any())
        {
            var mensaje = $"Se encontraron {errores.Count} error(es) de validación. No se insertó ningún registro.";
            _logger.LogWarning("⚠️ [SERVICE] {Mensaje}. Usuario: {Usuario}", mensaje, usuarioAlta);
            throw new BusinessValidationException(mensaje, errores);
        }

        // Si todas las validaciones pasan, proceder con el batch insert
        _logger.LogInformation(
            "✅ [SERVICE] Todas las validaciones pasaron. Procediendo con batch insert de {Cantidad} registros",
            entidades.Count);

        var entidadesCreadas = await _repository.CrearBatchAsync(entidades, usuarioAlta);

        _logger.LogInformation(
            "✅ [SERVICE] Batch insert completado exitosamente. {Cantidad} registros creados. Usuario: {Usuario}",
            entidadesCreadas.Count, usuarioAlta);

        // Para los registros creados, obtener datos completos con JOIN
        var dtos = new List<FotoDealerProductosDto>();
        foreach (var entidad in entidadesCreadas)
        {
            var resultado = await _repository.ObtenerPorIdCompletoAsync(entidad.FotoDealerProductosId);
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
    private static FotoDealerProductosDto MapearADto(FotoDealerProductosMap map)
    {
        return new FotoDealerProductosDto
        {
            FotoDealerProductosId = map.FotoDealerProductosId,
            CargaArchivoSincronizacionId = map.CargaArchivoSincronizacionId,
            IdCarga = map.IdCarga ?? string.Empty,
            ProcesoCarga = map.ProcesoCarga ?? string.Empty,
            FechaCarga = map.FechaCarga ?? DateTime.MinValue,
            FechaSincronizacion = map.FechaSincronizacion,
            TiempoSincronizacionHoras = map.TiempoSincronizacionHoras,
            DealerBac = map.DealerBac,
            NombreDealer = map.NombreDealer,
            RazonSocialDealer = map.RazonSocialDealer,
            Dms = map.Dms,
            FechaRegistro = map.FechaRegistro,
            FechaAlta = map.FechaAlta,
            UsuarioAlta = map.UsuarioAlta,
            FechaModificacion = map.FechaModificacion,
            UsuarioModificacion = map.UsuarioModificacion
        };
    }
}

