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
/// Implementación del servicio para Foto de Dealers Carga Archivos Sincronización.
/// </summary>
public class FotoDealersCargaArchivosSincService : IFotoDealersCargaArchivosSincService
{
    private readonly IFotoDealersCargaArchivosSincRepository _repository;
    private readonly IDistribuidorRepository _distribuidorRepository;
    private readonly ILogger<FotoDealersCargaArchivosSincService> _logger;

    public FotoDealersCargaArchivosSincService(
        IFotoDealersCargaArchivosSincRepository repository,
        IDistribuidorRepository distribuidorRepository,
        ILogger<FotoDealersCargaArchivosSincService> logger)
    {
        _repository = repository;
        _distribuidorRepository = distribuidorRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FotoDealersCargaArchivosSincDto?> ObtenerPorIdAsync(int id)
    {
        _logger.LogInformation("🔷 [SERVICE] Obteniendo foto dealers carga archivos sinc con ID {Id}", id);

        var resultado = await _repository.ObtenerPorIdCompletoAsync(id);

        if (resultado == null)
        {
            _logger.LogWarning("⚠️ [SERVICE] Foto dealers carga archivos sinc con ID {Id} no encontrado", id);
            return null;
        }

        _logger.LogInformation("✅ [SERVICE] Foto dealers carga archivos sinc con ID {Id} obtenido exitosamente", id);
        return MapearADto(resultado);
    }

    /// <inheritdoc />
    public async Task<(List<FotoDealersCargaArchivosSincDto> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        int? cargaArchivoSincronizacionId = null,
        string? dealerBac = null,
        string? dms = null,
        int? sincronizado = null,
        int page = 1,
        int pageSize = 200)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] Obteniendo fotos dealers carga archivos sinc con filtros. CargaArchivoSincId: {CargaId}, DealerBac: {DealerBac}, DMS: {Dms}, Sincronizado: {Sincronizado}, Página: {Page}, PageSize: {PageSize}",
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
    public async Task<List<FotoDealersCargaArchivosSincDto>> CrearBatchAsync(
        CrearFotoDealersCargaArchivosSincBatchDto dto,
        string usuarioAlta,
        int? empresaId = null,
        string? usuario = null)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] Iniciando creación batch automática de registros. CargaArchivoSincId: {CargaId}, EmpresaId: {EmpresaId}, Usuario: {Usuario}",
            dto.CargaArchivoSincronizacionId, empresaId?.ToString() ?? "Todos", usuario ?? "Todos");

        // VALIDACIÓN: Verificar que existe el CargaArchivoSincronizacionId
        var existeCarga = await _repository.ExisteCargaArchivoSincronizacionIdAsync(dto.CargaArchivoSincronizacionId);
        if (!existeCarga)
        {
            _logger.LogWarning("⚠️ [SERVICE] No existe un registro de carga de archivo de sincronización con ID {CargaId}. Usuario: {Usuario}",
                dto.CargaArchivoSincronizacionId, usuarioAlta);
            throw new BusinessValidationException(
                $"No existe un registro de carga de archivo de sincronización con ID {dto.CargaArchivoSincronizacionId}",
                new List<ValidationError>
                {
                    new ValidationError
                    {
                        Field = "cargaArchivoSincronizacionId",
                        Message = $"No existe un registro de carga de archivo de sincronización con ID {dto.CargaArchivoSincronizacionId}",
                        AttemptedValue = dto.CargaArchivoSincronizacionId
                    }
                });
        }

        // Obtener todos los distribuidores desde CO_DISTRIBUIDORES
        _logger.LogInformation(
            "🔷 [SERVICE] Consultando distribuidores desde CO_DISTRIBUIDORES. EmpresaId: {EmpresaId}, Usuario: {Usuario}",
            empresaId?.ToString() ?? "Todos", usuario ?? "Todos");

        var distribuidores = await _distribuidorRepository.ObtenerTodosAsync(empresaId, usuario);

        if (distribuidores == null || !distribuidores.Any())
        {
            _logger.LogWarning("⚠️ [SERVICE] No se encontraron distribuidores. EmpresaId: {EmpresaId}, Usuario: {Usuario}",
                empresaId?.ToString() ?? "Todos", usuario ?? "Todos");
            throw new BusinessValidationException(
                "No se encontraron distribuidores para generar los registros",
                new List<ValidationError>());
        }

        _logger.LogInformation(
            "✅ [SERVICE] Se encontraron {Cantidad} distribuidores. Generando registros automáticamente...",
            distribuidores.Count);

        // Generar entidades automáticamente para cada distribuidor
        var fechaRegistro = DateTimeHelper.GetMexicoDateTime();
        var entidades = distribuidores.Select(distribuidor => new FotoDealersCargaArchivosSinc
        {
            CargaArchivoSincronizacionId = dto.CargaArchivoSincronizacionId,
            DealerBac = distribuidor.DealerBac,
            NombreDealer = distribuidor.NombreDealer ?? distribuidor.Nombre,
            RazonSocialDealer = distribuidor.RazonSocial,
            Dms = string.IsNullOrWhiteSpace(distribuidor.Dms) ? "GDMS" : distribuidor.Dms, // Default "GDMS" si está vacío
            FechaRegistro = fechaRegistro,
            FechaAlta = DateTimeHelper.GetMexicoDateTime(),
            UsuarioAlta = usuarioAlta
        }).ToList();

        _logger.LogInformation(
            "✅ [SERVICE] Se generaron {Cantidad} registros automáticamente desde {CantidadDistribuidores} distribuidores",
            entidades.Count, distribuidores.Count);

        // VALIDACIÓN PREVIA: Verificar duplicados en la base de datos (ANTES de insertar)
        var errores = new List<ValidationError>();
        var registrosExistentes = 0;
        var entidadesNuevas = new List<FotoDealersCargaArchivosSinc>();

        foreach (var entidad in entidades)
        {
            var existe = await _repository.ExisteCombinacionAsync(
                entidad.CargaArchivoSincronizacionId,
                entidad.DealerBac);

            if (existe)
            {
                registrosExistentes++;
                // No agregar error, solo registrar que ya existe (se omitirá en el insert)
                _logger.LogDebug(
                    "⚠️ [SERVICE] Ya existe registro para CargaArchivoSincId={CargaId} y DealerBac={DealerBac}. Se omitirá.",
                    entidad.CargaArchivoSincronizacionId, entidad.DealerBac);
            }
            else
            {
                entidadesNuevas.Add(entidad);
            }
        }

        if (!entidadesNuevas.Any())
        {
            _logger.LogWarning(
                "⚠️ [SERVICE] Todos los registros ya existen en la base de datos. No se insertará ningún registro nuevo. Usuario: {Usuario}",
                usuarioAlta);
            throw new BusinessValidationException(
                $"Todos los {entidades.Count} distribuidores ya tienen un registro para esta carga de archivo. No se insertó ningún registro nuevo.",
                new List<ValidationError>());
        }

        _logger.LogInformation(
            "✅ [SERVICE] De {Total} distribuidores, {Nuevos} son nuevos y {Existentes} ya existen. Se insertarán {Nuevos} registros.",
            entidades.Count, entidadesNuevas.Count, registrosExistentes, entidadesNuevas.Count);

        // Si todas las validaciones pasan, proceder con el batch insert
        var entidadesCreadas = await _repository.CrearBatchAsync(entidadesNuevas, usuarioAlta);

        _logger.LogInformation(
            "✅ [SERVICE] Batch insert completado exitosamente. {Cantidad} registros creados de {Total} distribuidores. Usuario: {Usuario}",
            entidadesCreadas.Count, distribuidores.Count, usuarioAlta);

        // Para los registros creados, obtener datos completos con JOIN
        var dtos = new List<FotoDealersCargaArchivosSincDto>();
        foreach (var entidad in entidadesCreadas)
        {
            var resultado = await _repository.ObtenerPorIdCompletoAsync(entidad.FotoDealersCargaArchivosSincId);
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
    private static FotoDealersCargaArchivosSincDto MapearADto(FotoDealersCargaArchivosSincMap map)
    {
        return new FotoDealersCargaArchivosSincDto
        {
            FotoDealersCargaArchivosSincId = map.FotoDealersCargaArchivosSincId,
            CargaArchivoSincronizacionId = map.CargaArchivoSincronizacionId,
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
            UsuarioModificacion = map.UsuarioModificacion
        };
    }
}

