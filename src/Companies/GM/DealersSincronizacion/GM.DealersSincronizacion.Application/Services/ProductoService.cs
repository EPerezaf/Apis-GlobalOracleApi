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
    private readonly ILogger<ProductoService> _logger;

    public ProductoService(
        IProductoRepository repository,
        ILogger<ProductoService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(List<ProductoDto> data, int totalRecords)> ObtenerTodosAsync(
        int page = 1,
        int pageSize = 200)
    {
        _logger.LogInformation("🔷 [SERVICE] Obteniendo productos activos. Página: {Page}, PageSize: {PageSize}", page, pageSize);

        var (productos, totalRecords) = await _repository.ObtenerTodosAsync(page, pageSize);

        var dtos = productos.Select(p => new ProductoDto
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

        _logger.LogInformation("✅ [SERVICE] {Cantidad} productos obtenidos de {Total} totales", dtos.Count, totalRecords);
        return (dtos, totalRecords);
    }
}

