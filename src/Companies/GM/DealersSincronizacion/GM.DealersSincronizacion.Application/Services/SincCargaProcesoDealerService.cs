using GM.CatalogSync.Domain.Entities;
using GM.DealersSincronizacion.Application.DTOs;
using GM.DealersSincronizacion.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Security;
using ValidationError = Shared.Exceptions.ValidationError;

namespace GM.DealersSincronizacion.Application.Services;

/// <summary>
/// Servicio de sincronización de carga de proceso por dealer.
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
    public async Task<SincCargaProcesoDealerDto> CrearAsync(CrearSincCargaProcesoDealerDto dto, string dealerBac, string usuarioAlta)
    {
        _logger.LogInformation("🔷 [SERVICE] Creando registro de sincronización. DealerBac: {DealerBac}, EventoCargaProcesoId: {EventoCargaProcesoId}",
            dealerBac, dto.EventoCargaProcesoId);

        // Validar que existe el evento de carga de proceso y que es actual
        var evento = await _eventoCargaProcesoRepository.ObtenerPorIdAsync(dto.EventoCargaProcesoId);
        if (evento == null || !evento.Actual)
        {
            _logger.LogWarning("⚠️ [SERVICE] No se encontró un registro de evento de carga activo con EventoCargaProcesoId: {EventoCargaProcesoId}. DealerBac: {DealerBac}",
                dto.EventoCargaProcesoId, dealerBac);
            throw new BusinessValidationException(
                $"No se encontró un registro de evento de carga activo con EventoCargaProcesoId {dto.EventoCargaProcesoId}",
                new List<ValidationError>());
        }

        // Verificar si ya existe un registro para este dealer y evento de carga
        var existente = await _repository.ObtenerPorCargaYDealerAsync(dto.EventoCargaProcesoId, dealerBac);
        if (existente != null)
        {
            var fechaSinc = existente.FechaSincronizacion.ToString("dd/MM/yyyy HH:mm:ss");
            _logger.LogWarning("⚠️ [SERVICE] Ya existe un registro de sincronización para este dealer y evento de carga. Fecha: {Fecha}", fechaSinc);
            throw new BusinessValidationException(
                $"Ya existe un registro de sincronización para este dealer y este evento de carga de proceso. Fecha de sincronización previa: {fechaSinc}",
                new List<ValidationError>());
        }

        // Consultar información del distribuidor desde CO_DISTRIBUIDORES
        var distribuidor = await _distribuidorRepository.ObtenerPorDealerBacAsync(dealerBac);
        if (distribuidor == null)
        {
            _logger.LogWarning("⚠️ [SERVICE] No se encontró el distribuidor con DealerBac: {DealerBac}", dealerBac);
            throw new NotFoundException($"No se encontró el distribuidor con DealerBac: {dealerBac}", "Distribuidor", dealerBac);
        }

        _logger.LogInformation("✅ [SERVICE] Distribuidor encontrado. Nombre: {Nombre}, DMS: {Dms}",
            distribuidor.NombreDealer ?? distribuidor.Nombre, distribuidor.Dms);

        // Calcular fecha de sincronización
        var fechaSincronizacion = DateTimeHelper.GetMexicoDateTime();

        // Generar token de confirmación: SHA256(idCarga + dealerBac + proceso + fechaSincronizacion + registrosSincronizados)
        var tokenConfirmacion = HashHelper.GenerateTokenConfirmacion(
            evento.IdCarga,
            dealerBac,
            evento.Proceso.Trim(),
            fechaSincronizacion,
            evento.Registros);

        _logger.LogInformation(
            "🔐 [SERVICE] Token de confirmación generado. IdCarga: {IdCarga}, DealerBac: {DealerBac}, Proceso: {Proceso}, Token: {Token}",
            evento.IdCarga, dealerBac, evento.Proceso, tokenConfirmacion);

        // Crear entidad con datos del distribuidor y el evento de carga
        // NOTA: proceso y registrosSincronizados se obtienen del evento (CO_EVENTOSCARGAPROCESO)
        var entidad = new SincCargaProcesoDealer
        {
            Proceso = evento.Proceso, // ✅ Obtenido de CO_EVENTOSCARGAPROCESO.COCP_PROCESO
            EventoCargaProcesoId = dto.EventoCargaProcesoId,
            DealerBac = dealerBac,
            NombreDealer = distribuidor.NombreDealer ?? distribuidor.Nombre,
            DmsOrigen = string.IsNullOrWhiteSpace(distribuidor.Dms) ? "GDMS" : distribuidor.Dms, // ✅ Valor por defecto "GDMS" si está vacío
            FechaSincronizacion = fechaSincronizacion, // Calculado automáticamente (hora de México)
            RegistrosSincronizados = evento.Registros, // ✅ Obtenido de CO_EVENTOSCARGAPROCESO.COCP_REGISTROS
            TokenConfirmacion = tokenConfirmacion // ✅ Generado automáticamente con SHA256
        };

        // Guardar en repositorio
        var resultado = await _repository.CrearAsync(entidad, usuarioAlta);

        // Calcular tiempo de sincronización en horas (diferencia entre FechaSincronizacion y FechaCarga)
        // Siempre redondear a 2 decimales para mayor precisión
        var tiempoSincronizacionHoras = 0.00m;
        if (evento.FechaCarga != DateTime.MinValue && resultado.FechaSincronizacion != DateTime.MinValue)
        {
            var diferencia = resultado.FechaSincronizacion - evento.FechaCarga;
            tiempoSincronizacionHoras = Math.Round((decimal)diferencia.TotalHours, 2, MidpointRounding.AwayFromZero);
        }

        var resultadoDto = new SincCargaProcesoDealerDto
        {
            SincCargaProcesoDealerId = resultado.SincCargaProcesoDealerId,
            Proceso = resultado.Proceso,
            EventoCargaProcesoId = resultado.EventoCargaProcesoId,
            DmsOrigen = resultado.DmsOrigen,
            DealerBac = resultado.DealerBac,
            NombreDealer = resultado.NombreDealer,
            FechaSincronizacion = resultado.FechaSincronizacion,
            RegistrosSincronizados = resultado.RegistrosSincronizados,
            TokenConfirmacion = resultado.TokenConfirmacion,
            TiempoSincronizacionHoras = tiempoSincronizacionHoras
        };

        _logger.LogInformation("✅ [SERVICE] Registro de sincronización creado exitosamente. ID: {Id}, TiempoSincronizacion: {TiempoHoras} horas", 
            resultadoDto.SincCargaProcesoDealerId, tiempoSincronizacionHoras);
        return resultadoDto;
    }
}

