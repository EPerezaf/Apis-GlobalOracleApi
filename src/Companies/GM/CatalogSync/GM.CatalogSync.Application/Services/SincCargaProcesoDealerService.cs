using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Security;

namespace GM.CatalogSync.Application.Services;

/// <summary>
/// Implementación del servicio para Sincronización de Carga de Proceso por Dealer.
/// </summary>
public class SincCargaProcesoDealerService : ISincCargaProcesoDealerService
{
    private readonly ISincCargaProcesoDealerRepository _repository;
    private readonly IEventoCargaProcesoRepository _eventoCargaProcesoRepository;
    private readonly IDistribuidorRepository _distribuidorRepository;
    private readonly ILogger<SincCargaProcesoDealerService> _logger;

    public SincCargaProcesoDealerService(
        ISincCargaProcesoDealerRepository repository,
        IEventoCargaProcesoRepository eventoCargaProcesoRepository,
        IDistribuidorRepository distribuidorRepository,
        ILogger<SincCargaProcesoDealerService> logger)
    {
        _repository = repository;
        _eventoCargaProcesoRepository = eventoCargaProcesoRepository;
        _distribuidorRepository = distribuidorRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SincCargaProcesoDealerDto?> ObtenerPorIdAsync(int id)
    {
        _logger.LogInformation("🔷 [SERVICE] Obteniendo registro de sincronización con ID {Id}", id);

        var entidad = await _repository.ObtenerPorIdAsync(id);

        if (entidad == null)
        {
            _logger.LogWarning("⚠️ [SERVICE] Registro de sincronización con ID {Id} no encontrado", id);
            return null;
        }

        // Obtener datos del evento de carga
        var evento = await _eventoCargaProcesoRepository.ObtenerPorIdAsync(entidad.EventoCargaProcesoId);

        _logger.LogInformation("✅ [SERVICE] Registro de sincronización con ID {Id} obtenido exitosamente", id);
        return MapearADto(entidad, evento);
    }

    /// <inheritdoc />
    public async Task<(List<SincCargaProcesoDealerDto> data, int totalRecords)> ObtenerTodosConFiltrosAsync(
        string? proceso = null,
        int? eventoCargaProcesoId = null,
        string? dealerBac = null,
        int page = 1,
        int pageSize = 200)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] Obteniendo registros de sincronización con filtros. Proceso: {Proceso}, EventoCargaProcesoId: {EventoCargaProcesoId}, DealerBac: {DealerBac}, Página: {Page}, PageSize: {PageSize}",
            proceso ?? "null", eventoCargaProcesoId?.ToString() ?? "null", dealerBac ?? "null", page, pageSize);

        var (entidades, totalRecords) = await _repository.ObtenerTodosConFiltrosAsync(proceso, eventoCargaProcesoId, dealerBac, page, pageSize);

        _logger.LogInformation(
            "✅ [SERVICE] Se obtuvieron {Cantidad} registros de sincronización de {Total} totales (Página {Page})",
            entidades.Count, totalRecords, page);

        // Obtener datos de los eventos en batch
        var eventoIds = entidades.Select(e => e.EventoCargaProcesoId).Distinct().ToList();
        var eventos = new Dictionary<int, EventoCargaProceso?>();
        foreach (var eventoId in eventoIds)
        {
            var evento = await _eventoCargaProcesoRepository.ObtenerPorIdAsync(eventoId);
            eventos[eventoId] = evento;
        }

        var dtos = entidades.Select(e => MapearADto(e, eventos.GetValueOrDefault(e.EventoCargaProcesoId))).ToList();
        return (dtos, totalRecords);
    }

    /// <inheritdoc />
    public async Task<SincCargaProcesoDealerDto> CrearAsync(CrearSincCargaProcesoDealerDto dto, string usuarioAlta, string dealerBac)
    {
        _logger.LogInformation(
            "🔷 [SERVICE] Iniciando creación de registro de sincronización. EventoCargaProcesoId: {EventoCargaProcesoId}, DealerBac: {DealerBac}, Usuario: {Usuario}",
            dto.EventoCargaProcesoId, dealerBac, usuarioAlta);

        // Validar datos requeridos
        if (dto.EventoCargaProcesoId <= 0)
        {
            throw new SincArchivoDealerValidacionException("El ID de evento de carga de proceso es requerido y debe ser mayor a 0");
        }

        if (string.IsNullOrWhiteSpace(dealerBac))
        {
            throw new SincArchivoDealerValidacionException("El código BAC del dealer es requerido");
        }

        // Validar que existe el EventoCargaProcesoId y obtener datos del evento
        var evento = await _eventoCargaProcesoRepository.ObtenerPorIdAsync(dto.EventoCargaProcesoId);
        if (evento == null || !evento.Actual)
        {
            _logger.LogWarning(
                "⚠️ [SERVICE] No se encontró un registro de evento de carga activo con EventoCargaProcesoId: {EventoCargaProcesoId}. Usuario: {Usuario}",
                dto.EventoCargaProcesoId, usuarioAlta);
            throw new NotFoundException(
                $"No se encontró un registro de evento de carga activo con EventoCargaProcesoId {dto.EventoCargaProcesoId}",
                "EventoCargaProceso",
                dto.EventoCargaProcesoId.ToString());
        }

        // Obtener información del distribuidor desde CO_DISTRIBUIDORES
        var distribuidor = await _distribuidorRepository.ObtenerPorDealerBacAsync(dealerBac.Trim());
        if (distribuidor == null)
        {
            _logger.LogWarning("⚠️ [SERVICE] No se encontró el distribuidor con DealerBac: {DealerBac}", dealerBac);
            throw new NotFoundException($"No se encontró el distribuidor con DealerBac: {dealerBac}", "Distribuidor", dealerBac);
        }

        _logger.LogInformation("✅ [SERVICE] Distribuidor encontrado. Nombre: {Nombre}, DMS: {Dms}",
            distribuidor.NombreDealer ?? distribuidor.Nombre, distribuidor.Dms);

        // Obtener proceso del evento
        var proceso = evento.Proceso;

        // Validar que no exista duplicado (proceso + eventoCargaProcesoId + dealerBac)
        var registroExistente = await _repository.ObtenerPorProcesoCargaYDealerAsync(
            proceso.Trim(), 
            dto.EventoCargaProcesoId, 
            dealerBac.Trim());

        if (registroExistente != null)
        {
            var fechaSinc = registroExistente.FechaSincronizacion.ToString("dd/MM/yyyy HH:mm:ss");
            _logger.LogWarning(
                "⚠️ [SERVICE] Ya existe un registro con Proceso: '{Proceso}', EventoCargaProcesoId: {EventoCargaProcesoId}, DealerBac: '{DealerBac}'. Fecha de sincronización previa: {Fecha}. Usuario: {Usuario}",
                proceso, dto.EventoCargaProcesoId, dealerBac, fechaSinc, usuarioAlta);
            throw new SincArchivoDealerDuplicadoException(proceso, dto.EventoCargaProcesoId, dealerBac, registroExistente.FechaSincronizacion);
        }

        // Calcular fecha de sincronización
        var fechaSincronizacion = DateTimeHelper.GetMexicoDateTime();

        // Generar token de confirmación: SHA256(idCarga + dealerBac + proceso + fechaSincronizacion + registrosSincronizados)
        var tokenConfirmacion = HashHelper.GenerateTokenConfirmacion(
            evento.IdCarga,
            dealerBac.Trim(),
            proceso.Trim(),
            fechaSincronizacion,
            evento.Registros);

        _logger.LogInformation(
            "🔐 [SERVICE] Token de confirmación generado. IdCarga: {IdCarga}, DealerBac: {DealerBac}, Proceso: {Proceso}, Token: {Token}",
            evento.IdCarga, dealerBac.Trim(), proceso, tokenConfirmacion);

        // Crear entidad con datos obtenidos del evento y distribuidor
        var entidad = new SincCargaProcesoDealer
        {
            Proceso = proceso.Trim(), // ✅ Obtenido de CO_EVENTOSCARGAPROCESO.COCP_PROCESO
            EventoCargaProcesoId = dto.EventoCargaProcesoId,
            DmsOrigen = string.IsNullOrWhiteSpace(distribuidor.Dms) ? "GDMS" : distribuidor.Dms, // ✅ Obtenido de CO_DISTRIBUIDORES.CODI_DMS
            DealerBac = dealerBac.Trim(),
            NombreDealer = distribuidor.NombreDealer ?? distribuidor.Nombre, // ✅ Obtenido de CO_DISTRIBUIDORES.CODI_NOMBRE
            FechaSincronizacion = fechaSincronizacion, // Calculado automáticamente (hora de México)
            RegistrosSincronizados = evento.Registros, // ✅ Obtenido de CO_EVENTOSCARGAPROCESO.COCP_REGISTROS
            TokenConfirmacion = tokenConfirmacion // ✅ Generado automáticamente con SHA256
        };

        // Crear registro
        var entidadCreada = await _repository.CrearAsync(entidad, usuarioAlta);

        _logger.LogInformation(
            "✅ [SERVICE] Registro de sincronización creado exitosamente. ID: {Id}, Proceso: {Proceso}, DealerBac: {DealerBac}",
            entidadCreada.SincCargaProcesoDealerId, entidadCreada.Proceso, entidadCreada.DealerBac);

        // Obtener datos del evento para el DTO de respuesta
        return MapearADto(entidadCreada, evento);
    }

    /// <summary>
    /// Mapea una entidad a DTO.
    /// </summary>
    private static SincCargaProcesoDealerDto MapearADto(SincCargaProcesoDealer entidad, EventoCargaProceso? evento = null, decimal? tiempoSincronizacionHoras = null)
    {
        // Usar el tiempo calculado en SQL si está disponible, sino calcularlo
        // Siempre redondear a 2 decimales para mayor precisión
        decimal tiempoHoras = 0.00m;
        if (tiempoSincronizacionHoras.HasValue)
        {
            // Si viene de SQL, redondear nuevamente para asegurar 2 decimales
            tiempoHoras = Math.Round(tiempoSincronizacionHoras.Value, 2, MidpointRounding.AwayFromZero);
        }
        else if (evento != null && evento.FechaCarga != DateTime.MinValue && entidad.FechaSincronizacion != DateTime.MinValue)
        {
            var diferencia = entidad.FechaSincronizacion - evento.FechaCarga;
            tiempoHoras = Math.Round((decimal)diferencia.TotalHours, 2, MidpointRounding.AwayFromZero);
        }

        return new SincCargaProcesoDealerDto
        {
            SincCargaProcesoDealerId = entidad.SincCargaProcesoDealerId,
            Proceso = entidad.Proceso,
            EventoCargaProcesoId = entidad.EventoCargaProcesoId,
            IdCarga = evento?.IdCarga ?? string.Empty,
            ProcesoCarga = evento?.Proceso ?? string.Empty,
            FechaCarga = evento?.FechaCarga ?? DateTime.MinValue,
            TiempoSincronizacionHoras = tiempoHoras,
            DmsOrigen = entidad.DmsOrigen,
            DealerBac = entidad.DealerBac,
            NombreDealer = entidad.NombreDealer,
            FechaSincronizacion = entidad.FechaSincronizacion,
            RegistrosSincronizados = entidad.RegistrosSincronizados,
            TokenConfirmacion = entidad.TokenConfirmacion,
            FechaAlta = entidad.FechaAlta,
            UsuarioAlta = entidad.UsuarioAlta,
            FechaModificacion = entidad.FechaModificacion,
            UsuarioModificacion = entidad.UsuarioModificacion
        };
    }
}

