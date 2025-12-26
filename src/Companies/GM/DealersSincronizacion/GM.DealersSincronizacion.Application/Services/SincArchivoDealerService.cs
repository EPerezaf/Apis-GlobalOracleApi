using GM.CatalogSync.Domain.Entities;
using GM.DealersSincronizacion.Application.DTOs;
using GM.DealersSincronizacion.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.Security;
using ValidationError = Shared.Exceptions.ValidationError;

namespace GM.DealersSincronizacion.Application.Services;

/// <summary>
/// Servicio de sincronización de archivos por dealer.
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
    public async Task<SincArchivoDealerDto> CrearAsync(CrearSincArchivoDealerDto dto, string dealerBac, string usuarioAlta)
    {
        _logger.LogInformation("🔷 [SERVICE] Creando registro de sincronización. DealerBac: {DealerBac}, CargaId: {CargaId}",
            dealerBac, dto.CargaArchivoSincronizacionId);

        // Validar que existe la carga de archivo
        var carga = await _cargaArchivoSincRepository.ObtenerActualAsync();
        if (carga == null || carga.CargaArchivoSincronizacionId != dto.CargaArchivoSincronizacionId)
        {
            _logger.LogWarning("⚠️ [SERVICE] La carga de archivo especificada no existe o no es la actual");
            throw new BusinessValidationException("La carga de archivo especificada no existe o no es la actual", new List<ValidationError>());
        }

        // Verificar si ya existe un registro para este dealer y carga
        var existente = await _repository.ObtenerPorCargaYDealerAsync(dto.CargaArchivoSincronizacionId, dealerBac);
        if (existente != null)
        {
            var fechaSinc = existente.FechaSincronizacion.ToString("dd/MM/yyyy HH:mm:ss");
            _logger.LogWarning("⚠️ [SERVICE] Ya existe un registro de sincronización para este dealer y carga. Fecha: {Fecha}", fechaSinc);
            throw new BusinessValidationException(
                $"Ya existe un registro de sincronización para este dealer y esta carga de archivo. Fecha de sincronización previa: {fechaSinc}",
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

        // Crear entidad con datos del distribuidor y la carga
        // NOTA: proceso y registrosSincronizados se obtienen de la carga (CO_CARGAARCHIVOSINCRONIZACION)
        var entidad = new SincArchivoDealer
        {
            Proceso = carga.Proceso, // ✅ Obtenido de CO_CARGAARCHIVOSINCRONIZACION.COCA_PROCESO
            CargaArchivoSincronizacionId = dto.CargaArchivoSincronizacionId,
            DealerBac = dealerBac,
            NombreDealer = distribuidor.NombreDealer ?? distribuidor.Nombre,
            DmsOrigen = string.IsNullOrWhiteSpace(distribuidor.Dms) ? "GDMS" : distribuidor.Dms, // ✅ Valor por defecto "GDMS" si está vacío
            RegistrosSincronizados = carga.Registros // ✅ Obtenido de CO_CARGAARCHIVOSINCRONIZACION.COCA_REGISTROS
        };

        // Guardar en repositorio
        var resultado = await _repository.CrearAsync(entidad, usuarioAlta);

        var resultadoDto = new SincArchivoDealerDto
        {
            SincArchivoDealerId = resultado.SincArchivoDealerId,
            Proceso = resultado.Proceso,
            CargaArchivoSincronizacionId = resultado.CargaArchivoSincronizacionId,
            DmsOrigen = resultado.DmsOrigen,
            DealerBac = resultado.DealerBac,
            NombreDealer = resultado.NombreDealer,
            FechaSincronizacion = resultado.FechaSincronizacion,
            RegistrosSincronizados = resultado.RegistrosSincronizados
        };

        _logger.LogInformation("✅ [SERVICE] Registro de sincronización creado exitosamente. ID: {Id}", resultadoDto.SincArchivoDealerId);
        return resultadoDto;
    }
}

