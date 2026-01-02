using GM.DealersSincronizacion.Application.DTOs;

namespace GM.DealersSincronizacion.Application.Services;

/// <summary>
/// Interfaz del servicio de sincronización de carga de proceso por dealer.
/// </summary>
public interface ISincCargaProcesoDealerService
{
    /// <summary>
    /// Crea un nuevo registro de sincronización de carga de proceso por dealer.
    /// </summary>
    Task<SincCargaProcesoDealerDto> CrearAsync(CrearSincCargaProcesoDealerDto dto, string dealerBac, string usuarioAlta);
}

