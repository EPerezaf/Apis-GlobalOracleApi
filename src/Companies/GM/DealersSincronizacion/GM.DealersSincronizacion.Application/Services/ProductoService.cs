using GM.DealersSincronizacion.Application.DTOs;
using GM.DealersSincronizacion.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GM.DealersSincronizacion.Application.Services;

/// <summary>
/// Servicio de productos para dealers.
/// </summary>
public class ProductoService : IProductoService
{
    private readonly IProductoRepository _repository;
    private readonly IEventoCargaProcesoService _eventoCargaProcesoService;
    private readonly ILogger<ProductoService> _logger;

    public ProductoService(
        IProductoRepository repository,
        IEventoCargaProcesoService eventoCargaProcesoService,
        ILogger<ProductoService> logger)
    {
        _repository = repository;
        _eventoCargaProcesoService = eventoCargaProcesoService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(ProductosConCargaDto data, int totalRecords)> ObtenerTodosConCargaAsync(
        int page = 1,
        int pageSize = 200)
    {
        _logger.LogInformation("🔷 [SERVICE] Obteniendo productos activos con información de carga. Página: {Page}, PageSize: {PageSize}", page, pageSize);

        // Obtener productos
        var (productos, totalRecords) = await _repository.ObtenerTodosAsync(page, pageSize);

        var productosDto = productos.Select(p => new ProductoDto
        {
            ProductoId = p.ProductoId,
            NombreProducto = p.NombreProducto,
            Pais = p.Pais,
            NombreModelo = p.NombreModelo,
            AnioModelo = p.AnioModelo,
            ModeloInteres = p.ModeloInteres,
            MarcaNegocio = p.MarcaNegocio,
            NombreLocal = p.NombreLocal,
            DefinicionVehiculo = p.DefinicionVehiculo
        }).ToList();

        // Obtener información de evento de carga actual filtrada por proceso "ProductList"
        var eventoActual = await _eventoCargaProcesoService.ObtenerActualPorProcesoAsync("ProductList");

        var resultado = new ProductosConCargaDto
        {
            Productos = productosDto,
            EventoCargaProcesoId = eventoActual?.EventoCargaProcesoId ?? 0,
            Proceso = eventoActual?.Proceso ?? string.Empty,
            FechaCarga = eventoActual?.FechaCarga ?? DateTime.MinValue,
            IdCarga = eventoActual?.IdCarga ?? string.Empty,
            Registros = eventoActual?.Registros ?? 0,
            Actual = eventoActual?.Actual ?? false,
            TablaRelacion = eventoActual?.TablaRelacion
        };

        if (eventoActual == null)
        {
            _logger.LogWarning("⚠️ [SERVICE] No se encontró registro actual de evento de carga para proceso 'ProductList'. Los campos de carga estarán vacíos.");
        }
        else
        {
            _logger.LogInformation("✅ [SERVICE] {Cantidad} productos obtenidos de {Total} totales con información de carga (Proceso: {Proceso}, EventoCargaProcesoId: {EventoCargaProcesoId})", 
                productosDto.Count, totalRecords, eventoActual.Proceso, eventoActual.EventoCargaProcesoId);
        }
        
        return (resultado, totalRecords);
    }
}

