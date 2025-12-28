using GM.DealersSincronizacion.Application.DTOs;
using GM.DealersSincronizacion.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GM.DealersSincronizacion.Application.Services;

/// <summary>
/// Servicio de carga de archivo de sincronización para dealers.
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
    public async Task<CargaArchivoSincActualDto?> ObtenerActualAsync()
    {
        _logger.LogInformation("🔷 [SERVICE] Obteniendo registro actual de carga de archivo de sincronización");

        var carga = await _repository.ObtenerActualAsync();

        if (carga == null)
        {
            _logger.LogWarning("⚠️ [SERVICE] No se encontró registro actual de carga de archivo");
            return null;
        }

        var dto = new CargaArchivoSincActualDto
        {
            CargaArchivoSincronizacionId = carga.CargaArchivoSincronizacionId,
            Proceso = carga.Proceso,
            NombreArchivo = carga.NombreArchivo,
            FechaCarga = carga.FechaCarga,
            IdCarga = carga.IdCarga,
            Registros = carga.Registros,
            Actual = carga.Actual,
            TablaRelacion = carga.TablaRelacion
            // NOTA: DealersTotales, DealersSincronizados y PorcDealersSinc no se exponen a los dealers
        };

        _logger.LogInformation("✅ [SERVICE] Registro actual obtenido exitosamente. ID: {Id}", dto.CargaArchivoSincronizacionId);
        return dto;
    }

    /// <inheritdoc />
    public async Task<CargaArchivoSincActualDto?> ObtenerActualPorProcesoAsync(string proceso)
    {
        _logger.LogInformation("🔷 [SERVICE] Obteniendo registro actual de carga de archivo de sincronización para proceso: {Proceso}", proceso);

        var carga = await _repository.ObtenerActualPorProcesoAsync(proceso);

        if (carga == null)
        {
            _logger.LogWarning("⚠️ [SERVICE] No se encontró registro actual de carga de archivo para proceso: {Proceso}", proceso);
            return null;
        }

        var dto = new CargaArchivoSincActualDto
        {
            CargaArchivoSincronizacionId = carga.CargaArchivoSincronizacionId,
            Proceso = carga.Proceso,
            NombreArchivo = carga.NombreArchivo,
            FechaCarga = carga.FechaCarga,
            IdCarga = carga.IdCarga,
            Registros = carga.Registros,
            Actual = carga.Actual,
            TablaRelacion = carga.TablaRelacion
            // NOTA: DealersTotales, DealersSincronizados y PorcDealersSinc no se exponen a los dealers
        };

        _logger.LogInformation("✅ [SERVICE] Registro actual obtenido exitosamente para proceso {Proceso}. ID: {Id}", proceso, dto.CargaArchivoSincronizacionId);
        return dto;
    }
}

