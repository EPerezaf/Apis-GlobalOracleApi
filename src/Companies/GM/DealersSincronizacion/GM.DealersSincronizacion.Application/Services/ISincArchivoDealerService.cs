using GM.DealersSincronizacion.Application.DTOs;

namespace GM.DealersSincronizacion.Application.Services;

/// <summary>
/// Interfaz del servicio de sincronización de archivos por dealer.
/// </summary>
public interface ISincArchivoDealerService
{
    /// <summary>
    /// Crea un nuevo registro de sincronización de archivo por dealer.
    /// </summary>
    Task<SincArchivoDealerDto> CrearAsync(CrearSincArchivoDealerDto dto, string dealerBac, string usuarioAlta);
}

