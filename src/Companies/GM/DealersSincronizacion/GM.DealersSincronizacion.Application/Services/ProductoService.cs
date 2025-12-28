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
    private readonly ICargaArchivoSincService _cargaArchivoSincService;
    private readonly ILogger<ProductoService> _logger;

    public ProductoService(
        IProductoRepository repository,
        ICargaArchivoSincService cargaArchivoSincService,
        ILogger<ProductoService> logger)
    {
        _repository = repository;
        _cargaArchivoSincService = cargaArchivoSincService;
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

        // Obtener información de carga actual filtrada por proceso "ProductList"
        var cargaActual = await _cargaArchivoSincService.ObtenerActualPorProcesoAsync("ProductList");

        var resultado = new ProductosConCargaDto
        {
            Productos = productosDto,
            CargaArchivoSincronizacionId = cargaActual?.CargaArchivoSincronizacionId ?? 0,
            Proceso = cargaActual?.Proceso ?? string.Empty,
            FechaCarga = cargaActual?.FechaCarga ?? DateTime.MinValue,
            IdCarga = cargaActual?.IdCarga ?? string.Empty,
            Registros = cargaActual?.Registros ?? 0,
            Actual = cargaActual?.Actual ?? false,
            TablaRelacion = cargaActual?.TablaRelacion
        };

        if (cargaActual == null)
        {
            _logger.LogWarning("⚠️ [SERVICE] No se encontró registro actual de carga para proceso 'ProductList'. Los campos de carga estarán vacíos.");
        }
        else
        {
            _logger.LogInformation("✅ [SERVICE] {Cantidad} productos obtenidos de {Total} totales con información de carga (Proceso: {Proceso}, CargaId: {CargaId})", 
                productosDto.Count, totalRecords, cargaActual.Proceso, cargaActual.CargaArchivoSincronizacionId);
        }
        
        return (resultado, totalRecords);
    }
}

