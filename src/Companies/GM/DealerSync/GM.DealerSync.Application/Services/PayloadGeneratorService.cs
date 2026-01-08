using GM.DealerSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GM.DealerSync.Application.Services;

/// <summary>
/// Servicio para generar el payload del webhook según el tipo de proceso
/// </summary>
public class PayloadGeneratorService : IPayloadGeneratorService
{
    private readonly IProductoPayloadRepository _productoPayloadRepository;
    private readonly ICampaignPayloadRepository _campaignPayloadRepository;
    private readonly ILogger<PayloadGeneratorService> _logger;

    public PayloadGeneratorService(
        IProductoPayloadRepository productoPayloadRepository,
        ICampaignPayloadRepository campaignPayloadRepository,
        ILogger<PayloadGeneratorService> logger)
    {
        _productoPayloadRepository = productoPayloadRepository;
        _campaignPayloadRepository = campaignPayloadRepository;
        _logger = logger;
    }

    public async Task<object> GeneratePayloadAsync(
        string processType,
        int eventoCargaProcesoId,
        string idCarga,
        DateTime fechaCarga,
        int totalDealers)
    {
        _logger.LogInformation(
            "📦 [PAYLOAD] Generando payload para ProcessType: {ProcessType}, EventoCargaProcesoId: {EventoCargaProcesoId}",
            processType, eventoCargaProcesoId);

        // Generar payload según el tipo de proceso
        if (string.Equals(processType, "ProductList", StringComparison.OrdinalIgnoreCase))
        {
            var productos = await _productoPayloadRepository.GetAllProductosAsync();
            
            var listaProductos = productos.Select(p => new
            {
                nombreProducto = p.NombreProducto,
                pais = p.Pais,
                nombreModelo = p.NombreModelo,
                anioModelo = p.AnioModelo,
                modeloInteres = p.ModeloInteres,
                marcaNegocio = p.MarcaNegocio,
                nombreLocal = p.NombreLocal,
                definicionVehiculo = p.DefinicionVehiculo
            }).ToList();

            // Construir procesodetalle con el conteo correcto de registros
            var procesodetalle = new[]
            {
                new
                {
                    eventoCargaProcesoId = eventoCargaProcesoId,
                    proceso = processType,
                    fechaCarga = fechaCarga.ToString("yyyy-MM-ddTHH:mm:ss"), // Formato: 2025-12-31T13:13:54
                    idCarga = idCarga,
                    registros = listaProductos.Count,
                    webhooksTotales = totalDealers
                }
            };

            _logger.LogInformation(
                "✅ [PAYLOAD] Payload generado para ProductList - {CantidadProductos} productos, {WebhooksTotales} webhooks",
                listaProductos.Count, totalDealers);

            return new
            {
                procesodetalle = procesodetalle,
                listaProductos = listaProductos
            };
        }
        else if (string.Equals(processType, "CampaignList", StringComparison.OrdinalIgnoreCase))
        {
            var campaigns = await _campaignPayloadRepository.GetAllCampaignsAsync();
            
            var listaCampanias = campaigns.Select(c => new
            {
                sourceCodeId = c.SourceCodeId,
                id = c.Id,
                name = c.Name,
                recordTypeId = c.RecordTypeId,
                leadRecordType = c.LeadRecordType,
                leadEnquiryType = c.LeadEnquiryType,
                leadSource = c.LeadSource,
                leadSourceDetails = c.LeadSourceDetails,
                status = c.Status
            }).ToList();

            // Construir procesodetalle con el conteo correcto de registros
            var procesodetalle = new[]
            {
                new
                {
                    eventoCargaProcesoId = eventoCargaProcesoId,
                    proceso = processType,
                    fechaCarga = fechaCarga.ToString("yyyy-MM-ddTHH:mm:ss"), // Formato: 2025-12-31T13:13:54
                    idCarga = idCarga,
                    registros = listaCampanias.Count,
                    webhooksTotales = totalDealers
                }
            };

            _logger.LogInformation(
                "✅ [PAYLOAD] Payload generado para CampaignList - {CantidadCampanias} campañas, {WebhooksTotales} webhooks",
                listaCampanias.Count, totalDealers);

            return new
            {
                procesodetalle = procesodetalle,
                listaCampanias = listaCampanias
            };
        }
        else
        {
            _logger.LogWarning(
                "⚠️ [PAYLOAD] ProcessType no reconocido: {ProcessType}. Generando payload básico sin datos",
                processType);

            // Payload básico para procesos no reconocidos
            var procesodetalle = new[]
            {
                new
                {
                    eventoCargaProcesoId = eventoCargaProcesoId,
                    proceso = processType,
                    fechaCarga = fechaCarga.ToString("yyyy-MM-ddTHH:mm:ss"),
                    idCarga = idCarga,
                    registros = 0,
                    webhooksTotales = totalDealers
                }
            };

            return new
            {
                procesodetalle = procesodetalle
            };
        }
    }
}

