using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Security;

namespace GM.CatalogSync.Application.Services;

/// <summary>
/// Implementación del servicio para Sincronización de Archivos por Dealer.
/// </summary>
public class SincArchivoDealerService : ISincArchivoDealerService
{
    private readonly ISincArchivoDealerRepository _repository;
    private readonly ICargaArchivoSincRepository _cargaArchivoSincRepository;
    private readonly IDistribuidorRepository _distribuidorRepository;
    private readonly ILogger<SincArchivoDealerService> _logger;

    public SincArchivoDealerService(
        ISincArchivoDealerRepository repository,
        ICargaArchivoSincRepository cargaArchivoSincRepository,
        IDistribuidorRepository distribuidorRepository,
        ILogger<SincArchivoDealerService> logger)
    {
        _repository = repository;
        _cargaArchivoSincRepository = cargaArchivoSincRepository;
        _distribuidorRepository = distribuidorRepository;
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

        // Obtener datos de la carga
        var carga = await _cargaArchivoSincRepository.ObtenerPorIdAsync(entidad.CargaArchivoSincronizacionId);

        _logger.LogInformation("✅ [SERVICE] Registro de sincronización con ID {Id} obtenido exitosamente", id);
        return MapearADto(entidad, carga);
    }

    /// <inheritdoc />
    public async Task<(List<SincArchivoDealerDto> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        int? cargaArchivoSincronizacionId = null,
        string? dealerBac = null,
        int page = 1,
        int pageSize = 200)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] Obteniendo registros de sincronización con filtros. Proceso: {Proceso}, CargaArchivoSincronizacionId: {CargaArchivoSincronizacionId}, DealerBac: {DealerBac}, Página: {Page}, PageSize: {PageSize}",
            proceso ?? "null", cargaArchivoSincronizacionId?.ToString() ?? "null", dealerBac ?? "null", page, pageSize);

        var (entidades, totalRecords) = await _repository.ObtenerTodosConFiltrosAsync(proceso, cargaArchivoSincronizacionId, dealerBac, page, pageSize);

        _logger.LogInformation(
            "✅ [SERVICE] Se obtuvieron {Cantidad} registros de sincronización de {Total} totales (Página {Page})",
            entidades.Count, totalRecords, page);

        // Obtener datos de las cargas en batch
        var cargaIds = entidades.Select(e => e.CargaArchivoSincronizacionId).Distinct().ToList();
        var cargas = new Dictionary<int, CargaArchivoSincronizacion?>();
        foreach (var cargaId in cargaIds)
        {
            var carga = await _cargaArchivoSincRepository.ObtenerPorIdAsync(cargaId);
            cargas[cargaId] = carga;
        }

        var dtos = entidades.Select(e => MapearADto(e, cargas.GetValueOrDefault(e.CargaArchivoSincronizacionId))).ToList();
        return (dtos, totalRecords);
    }

    /// <inheritdoc />
    public async Task<SincArchivoDealerDto> CrearAsync(CrearSincArchivoDealerDto dto, string usuarioAlta)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] Iniciando creación de registro de sincronización. CargaArchivoSincronizacionId: {CargaArchivoSincronizacionId}, DealerBac: {DealerBac}, Usuario: {Usuario}",
            dto.CargaArchivoSincronizacionId, dto.DealerBac, usuarioAlta);

        // Validar datos requeridos
        if (dto.CargaArchivoSincronizacionId <= 0)
        {
            throw new SincArchivoDealerValidacionException("El ID de carga de archivo de sincronización es requerido y debe ser mayor a 0");
        }

        if (string.IsNullOrWhiteSpace(dto.DealerBac))
        {
            throw new SincArchivoDealerValidacionException("El código BAC del dealer es requerido");
        }

        // Validar que existe el CargaArchivoSincronizacionId y obtener datos de la carga
        var carga = await _cargaArchivoSincRepository.ObtenerPorIdAsync(dto.CargaArchivoSincronizacionId);
        if (carga == null || !carga.Actual)
        {
            _logger.LogWarning(
                "⚠️ [SERVICE] No se encontró un registro de carga activo con CargaArchivoSincronizacionId: {CargaArchivoSincronizacionId}. Usuario: {Usuario}",
                dto.CargaArchivoSincronizacionId, usuarioAlta);
            throw new NotFoundException(
                $"No se encontró un registro de carga activo con CargaArchivoSincronizacionId {dto.CargaArchivoSincronizacionId}",
                "CargaArchivoSincronizacion",
                dto.CargaArchivoSincronizacionId.ToString());
        }

        // Obtener información del distribuidor desde CO_DISTRIBUIDORES
        var distribuidor = await _distribuidorRepository.ObtenerPorDealerBacAsync(dto.DealerBac.Trim());
        if (distribuidor == null)
        {
            _logger.LogWarning("⚠️ [SERVICE] No se encontró el distribuidor con DealerBac: {DealerBac}", dto.DealerBac);
            throw new NotFoundException($"No se encontró el distribuidor con DealerBac: {dto.DealerBac}", "Distribuidor", dto.DealerBac);
        }

        _logger.LogInformation("✅ [SERVICE] Distribuidor encontrado. Nombre: {Nombre}, DMS: {Dms}",
            distribuidor.NombreDealer ?? distribuidor.Nombre, distribuidor.Dms);

        // Obtener proceso de la carga
        var proceso = carga.Proceso;

        // Validar que no exista duplicado (proceso + cargaArchivoSincronizacionId + dealerBac)
        var registroExistente = await _repository.ObtenerPorProcesoCargaYDealerAsync(
            proceso.Trim(), 
            dto.CargaArchivoSincronizacionId, 
            dto.DealerBac.Trim());

        if (registroExistente != null)
        {
            var fechaSinc = registroExistente.FechaSincronizacion.ToString("dd/MM/yyyy HH:mm:ss");
            _logger.LogWarning(
                "⚠️ [SERVICE] Ya existe un registro con Proceso: '{Proceso}', CargaArchivoSincronizacionId: {CargaArchivoSincronizacionId}, DealerBac: '{DealerBac}'. Fecha de sincronización previa: {Fecha}. Usuario: {Usuario}",
                proceso, dto.CargaArchivoSincronizacionId, dto.DealerBac, fechaSinc, usuarioAlta);
            throw new SincArchivoDealerDuplicadoException(proceso, dto.CargaArchivoSincronizacionId, dto.DealerBac, registroExistente.FechaSincronizacion);
        }

        // Crear entidad con datos obtenidos de la carga y distribuidor
        var entidad = new SincArchivoDealer
        {
            Proceso = proceso.Trim(), // ✅ Obtenido de CO_CARGAARCHIVOSINCRONIZACION.COCA_PROCESO
            CargaArchivoSincronizacionId = dto.CargaArchivoSincronizacionId,
            DmsOrigen = string.IsNullOrWhiteSpace(distribuidor.Dms) ? "GDMS" : distribuidor.Dms, // ✅ Obtenido de CO_DISTRIBUIDORES.CODI_DMS
            DealerBac = dto.DealerBac.Trim(),
            NombreDealer = distribuidor.NombreDealer ?? distribuidor.Nombre, // ✅ Obtenido de CO_DISTRIBUIDORES.CODI_NOMBRE
            FechaSincronizacion = DateTimeHelper.GetMexicoDateTime(), // Calculado automáticamente (hora de México)
            RegistrosSincronizados = carga.Registros // ✅ Obtenido de CO_CARGAARCHIVOSINCRONIZACION.COCA_REGISTROS
        };

        // Crear registro
        var entidadCreada = await _repository.CrearAsync(entidad, usuarioAlta);

        _logger.LogInformation(
            "✅ [SERVICE] Registro de sincronización creado exitosamente. ID: {Id}, Proceso: {Proceso}, DealerBac: {DealerBac}",
            entidadCreada.SincArchivoDealerId, entidadCreada.Proceso, entidadCreada.DealerBac);

        // Obtener datos de la carga para el DTO de respuesta
        return MapearADto(entidadCreada, carga);
    }

    /// <summary>
    /// Mapea una entidad a DTO.
    /// </summary>
    private static SincArchivoDealerDto MapearADto(SincArchivoDealer entidad, CargaArchivoSincronizacion? carga = null, decimal? tiempoSincronizacionHoras = null)
    {
        // Usar el tiempo calculado en SQL si está disponible, sino calcularlo
        decimal tiempoHoras = tiempoSincronizacionHoras ?? 0;
        if (tiempoHoras == 0 && carga != null && carga.FechaCarga != DateTime.MinValue && entidad.FechaSincronizacion != DateTime.MinValue)
        {
            var diferencia = entidad.FechaSincronizacion - carga.FechaCarga;
            tiempoHoras = Math.Round((decimal)diferencia.TotalHours, 2);
        }

        return new SincArchivoDealerDto
        {
            SincArchivoDealerId = entidad.SincArchivoDealerId,
            Proceso = entidad.Proceso,
            CargaArchivoSincronizacionId = entidad.CargaArchivoSincronizacionId,
            IdCarga = carga?.IdCarga ?? string.Empty,
            ProcesoCarga = carga?.Proceso ?? string.Empty,
            FechaCarga = carga?.FechaCarga ?? DateTime.MinValue,
            TiempoSincronizacionHoras = tiempoHoras,
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

