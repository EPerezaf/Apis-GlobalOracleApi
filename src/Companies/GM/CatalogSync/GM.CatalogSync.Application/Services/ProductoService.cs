using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Shared.Exceptions;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace GM.CatalogSync.Application.Services;

/// <summary>
/// Service para lógica de negocio de Producto
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

    public async Task<(List<ProductoRespuestaDto> data, int totalRecords)> ObtenerProductosAsync(
        string? pais,
        string? marcaNegocio,
        int? anioModelo,
        int page,
        int pageSize,
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[{CorrelationId}] 🔷 [SERVICE] Iniciando ObtenerProductosAsync - País: {Pais}, Marca: {Marca}, Año: {Anio}, Página: {Page}/{PageSize}",
                correlationId, pais ?? "Todos", marcaNegocio ?? "Todas", anioModelo?.ToString() ?? "Todos", page, pageSize);

            // Validar parámetros de paginación
            if (page < 1)
                throw new ProductoValidacionException("El número de página debe ser mayor o igual a 1",
                    new List<ValidationError> { new ValidationError { Field = "page", Message = "debe ser >= 1" } });

            if (pageSize < 1 || pageSize > 50000)
                throw new ProductoValidacionException("El tamaño de página debe estar entre 1 y 50000",
                    new List<ValidationError> { new ValidationError { Field = "pageSize", Message = "debe estar entre 1 y 50000" } });

            // Consultar desde Repository
            var (productos, totalRecords) = await _repository.GetByFiltersAsync(
                pais, marcaNegocio, anioModelo, page, pageSize, correlationId);

            // Mapear a DTOs
            var responseDtos = productos.Select(p => new ProductoRespuestaDto
            {
                ProductoId = p.ProductoId,
                NombreProducto = p.NombreProducto,
                Pais = p.Pais,
                NombreModelo = p.NombreModelo,
                AnioModelo = p.AnioModelo,
                ModeloInteres = p.ModeloInteres,
                MarcaNegocio = p.MarcaNegocio,
                NombreLocal = p.NombreLocal,
                DefinicionVehiculo = p.DefinicionVehiculo,
                FechaAlta = p.FechaAlta,
                UsuarioAlta = p.UsuarioAlta,
                FechaModificacion = p.FechaModificacion,
                UsuarioModificacion = p.UsuarioModificacion
            }).ToList();

            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] ✅ [SERVICE] ObtenerProductosAsync completado en {Tiempo}ms - {Count} registros de {Total} totales",
                correlationId, stopwatch.ElapsedMilliseconds, responseDtos.Count, totalRecords);

            return (responseDtos, totalRecords);
        }
        catch (ProductoValidacionException)
        {
            throw;
        }
        catch (ProductoDataAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{CorrelationId}] ❌ [SERVICE] Error inesperado en ObtenerProductosAsync", correlationId);
            throw new BusinessException("Error al obtener productos", ex);
        }
    }

    public async Task<ProductoBatchResultadoDto> ProcesarBatchInsertAsync(
        List<ProductoCrearDto> productos,
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[{CorrelationId}] 🔷 [SERVICE] Iniciando ProcesarBatchInsertAsync - Usuario: {User}, Total: {Count}",
                correlationId, currentUser, productos?.Count ?? 0);

            // Validar batch
            if (productos == null || productos.Count == 0)
                throw new ProductoValidacionException("La lista de productos no puede estar vacía",
                    new List<ValidationError> { new ValidationError { Field = "json", Message = "debe contener al menos un producto" } });

            if (productos.Count > 10000)
                throw new ProductoValidacionException("El tamaño del lote no puede exceder 10000 registros",
                    new List<ValidationError> { new ValidationError { Field = "json", Message = "máximo 10000 registros" } });

            // Validar campos obligatorios
            var erroresValidacion = ValidarRegistrosBatch(productos, correlationId);
            if (erroresValidacion.Any())
            {
                throw new ProductoValidacionException(
                    $"Se encontraron {erroresValidacion.Count} errores de validación",
                    erroresValidacion);
            }

            // Detectar duplicados en el lote
            var duplicados = DetectarDuplicadosEnBatch(productos, correlationId);
            if (duplicados.Any())
            {
                throw new ProductoDuplicadoException($"Se encontraron {duplicados.Count} productos duplicados en el lote", duplicados.Count, duplicados);
            }

            // Validar duplicados contra la base de datos
            var duplicadosEnBD = await ValidarDuplicadosEnBaseDatosAsync(productos, correlationId);
            if (duplicadosEnBD.Any())
            {
                var mensaje = $"Se encontraron {duplicadosEnBD.Count} registro(s) duplicado(s) que ya existen en la base de datos. " +
                              $"La combinación de nombreProducto, anioModelo y nombreLocal debe ser única.";
                throw new ProductoDuplicadoException(mensaje, duplicadosEnBD.Count);
            }

            // Mapear DTOs a Entities
            var productoEntities = productos.Select(p => new Producto
            {
                NombreProducto = p.NombreProducto.Trim(),
                Pais = p.Pais.Trim(),
                NombreModelo = p.NombreModelo.Trim(),
                AnioModelo = p.AnioModelo,
                ModeloInteres = p.ModeloInteres.Trim(),
                MarcaNegocio = p.MarcaNegocio.Trim(),
                NombreLocal = string.IsNullOrWhiteSpace(p.NombreLocal) ? null : p.NombreLocal.Trim(),
                DefinicionVehiculo = string.IsNullOrWhiteSpace(p.DefinicionVehiculo) ? null : p.DefinicionVehiculo.Trim()
            }).ToList();

            // Ejecutar INSERT batch
            var registrosInsertados = await _repository.UpsertBatchWithTransactionAsync(
                productoEntities, currentUser, correlationId);

            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] ✅ [SERVICE] ProcesarBatchInsertAsync completado en {Tiempo}ms - Insertados: {Ins}",
                correlationId, stopwatch.ElapsedMilliseconds, registrosInsertados);

            return new ProductoBatchResultadoDto
            {
                RegistrosTotales = productos.Count,
                RegistrosInsertados = registrosInsertados,
                RegistrosActualizados = 0,
                RegistrosError = productos.Count - registrosInsertados,
                OmitidosPorError = 0
            };
        }
        catch (ProductoDuplicadoException)
        {
            throw;
        }
        catch (ProductoValidacionException)
        {
            throw;
        }
        catch (ProductoDataAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{CorrelationId}] ❌ [SERVICE] Error inesperado en ProcesarBatchInsertAsync", correlationId);
            throw new BusinessException("Error al procesar lote de productos", ex);
        }
    }

    public async Task<int> EliminarTodosAsync(
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[{CorrelationId}] 🔷 [SERVICE] Iniciando EliminarTodosAsync - Usuario: {User}",
                correlationId, currentUser);

            var rowsAffected = await _repository.DeleteAllAsync(currentUser, correlationId);

            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] ✅ [SERVICE] EliminarTodosAsync completado en {Tiempo}ms - {Rows} filas eliminadas",
                correlationId, stopwatch.ElapsedMilliseconds, rowsAffected);

            return rowsAffected;
        }
        catch (ProductoDataAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{CorrelationId}] ❌ [SERVICE] Error inesperado en EliminarTodosAsync", correlationId);
            throw new BusinessException("Error al eliminar productos", ex);
        }
    }

    #region Private Helpers

    private List<ValidationError> ValidarRegistrosBatch(List<ProductoCrearDto> productos, string correlationId)
    {
        var errors = new List<ValidationError>();

        for (int i = 0; i < productos.Count; i++)
        {
            var producto = productos[i];
            var index = i + 1;

            if (string.IsNullOrWhiteSpace(producto.NombreProducto))
                errors.Add(new ValidationError { Field = $"json[{index}].nombreProducto", Message = "es requerido" });

            if (string.IsNullOrWhiteSpace(producto.Pais))
                errors.Add(new ValidationError { Field = $"json[{index}].pais", Message = "es requerido" });

            if (string.IsNullOrWhiteSpace(producto.NombreModelo))
                errors.Add(new ValidationError { Field = $"json[{index}].nombreModelo", Message = "es requerido" });

            if (producto.AnioModelo < 1900 || producto.AnioModelo > 2100)
                errors.Add(new ValidationError { Field = $"json[{index}].anioModelo", Message = "debe estar entre 1900 y 2100" });

            if (string.IsNullOrWhiteSpace(producto.ModeloInteres))
                errors.Add(new ValidationError { Field = $"json[{index}].modeloInteres", Message = "es requerido" });

            if (string.IsNullOrWhiteSpace(producto.MarcaNegocio))
                errors.Add(new ValidationError { Field = $"json[{index}].marcaNegocio", Message = "es requerido" });
        }

        return errors;
    }

    private List<ProductoCrearDto> DetectarDuplicadosEnBatch(List<ProductoCrearDto> productos, string correlationId)
    {
        var normalizedRecords = productos
            .Select((r, idx) => new
            {
                Index = idx,
                OriginalRecord = r,
                NombreProductoNormalized = (r.NombreProducto ?? string.Empty).Trim(),
                AnioModelo = r.AnioModelo,
                NombreLocalNormalized = (r.NombreLocal ?? string.Empty).Trim()
            })
            .ToList();

        var duplicadosGrupos = normalizedRecords
            .GroupBy(r => new { r.NombreProductoNormalized, r.AnioModelo, r.NombreLocalNormalized })
            .Where(g => g.Count() > 1)
            .ToList();

        var duplicados = new List<ProductoCrearDto>();

        if (duplicadosGrupos.Any())
        {
            foreach (var grupo in duplicadosGrupos)
            {
                _logger.LogWarning("[{CorrelationId}] ⚠️ [SERVICE] Duplicados encontrados en batch - Producto: '{Producto}', Año: {Anio}, Local: '{Local}' - Cantidad: {Count}",
                    correlationId,
                    grupo.Key.NombreProductoNormalized,
                    grupo.Key.AnioModelo,
                    grupo.Key.NombreLocalNormalized ?? "NULL",
                    grupo.Count());

                foreach (var registro in grupo.Select(g => g.OriginalRecord))
                {
                    duplicados.Add(registro);
                }
            }
        }

        return duplicados;
    }

    private async Task<List<ProductoCrearDto>> ValidarDuplicadosEnBaseDatosAsync(
        List<ProductoCrearDto> productos,
        string correlationId)
    {
        var duplicados = new List<ProductoCrearDto>();

        foreach (var producto in productos)
        {
            var nombreProductoNormalizado = (producto.NombreProducto ?? string.Empty).Trim();
            var nombreLocalNormalizado = string.IsNullOrWhiteSpace(producto.NombreLocal) ? null : producto.NombreLocal.Trim();

            var existe = await _repository.ExistsByProductoAnioAndLocalAsync(
                nombreProductoNormalizado,
                producto.AnioModelo,
                nombreLocalNormalizado,
                correlationId);

            if (existe)
            {
                _logger.LogWarning("[{CorrelationId}] ⚠️ [SERVICE] Duplicado encontrado en BD - Producto: '{Producto}', Año: {Anio}, Local: '{Local}'",
                    correlationId, nombreProductoNormalizado, producto.AnioModelo, nombreLocalNormalizado ?? "NULL");
                duplicados.Add(producto);
            }
        }

        return duplicados;
    }

    #endregion
}

