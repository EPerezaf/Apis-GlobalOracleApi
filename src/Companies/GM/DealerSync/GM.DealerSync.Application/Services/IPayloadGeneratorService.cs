namespace GM.DealerSync.Application.Services;

/// <summary>
/// Servicio para generar el payload del webhook según el tipo de proceso
/// </summary>
public interface IPayloadGeneratorService
{
    /// <summary>
    /// Genera el payload completo para el webhook según el processType
    /// </summary>
    /// <param name="processType">Tipo de proceso (ProductList, CampaignList, etc.)</param>
    /// <param name="eventoCargaProcesoId">ID del evento de carga de proceso</param>
    /// <param name="idCarga">ID de la carga</param>
    /// <param name="fechaCarga">Fecha de carga</param>
    /// <param name="totalDealers">Total de dealers a sincronizar</param>
    /// <returns>Payload serializado como objeto dinámico</returns>
    Task<object> GeneratePayloadAsync(
        string processType,
        int eventoCargaProcesoId,
        string idCarga,
        DateTime fechaCarga,
        int totalDealers);
}

