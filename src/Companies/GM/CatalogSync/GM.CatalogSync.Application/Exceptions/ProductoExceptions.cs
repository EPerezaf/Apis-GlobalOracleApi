using Shared.Exceptions;
using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Exceptions;

public class ProductoNotFoundException : NotFoundException
{
    public ProductoNotFoundException(int productoId)
        : base($"No se encontró el producto con ID: {productoId}", "Producto", productoId.ToString())
    {
    }
}

public class ProductoDuplicadoException : BusinessException
{
    public int CantidadDuplicados { get; set; }
    public List<ProductoCrearDto> RegistrosDuplicados { get; set; }

    public ProductoDuplicadoException(int cantidadDuplicados)
        : base($"Se encontraron {cantidadDuplicados} productos duplicados en el lote", "PRODUCTO_DUPLICADO")
    {
        CantidadDuplicados = cantidadDuplicados;
        RegistrosDuplicados = new List<ProductoCrearDto>();
    }

    public ProductoDuplicadoException(string message, int cantidadDuplicados, List<ProductoCrearDto>? registrosDuplicados = null)
        : base(message, "PRODUCTO_DUPLICADO")
    {
        CantidadDuplicados = cantidadDuplicados;
        RegistrosDuplicados = registrosDuplicados ?? new List<ProductoCrearDto>();
    }
}

public class ProductoDataAccessException : DataAccessException
{
    public ProductoDataAccessException(string message, Exception innerException)
        : base($"Error de acceso a datos en Producto: {message}", innerException)
    {
    }
}

public class ProductoValidacionException : BusinessValidationException
{
    public ProductoValidacionException(string message, List<ValidationError> errors)
        : base(message, errors)
    {
    }
}

public class ProductoBatchException : BusinessException
{
    public int TotalRegistros { get; set; }
    public int CantidadErrores { get; set; }
    public int CantidadExitosos { get; set; }

    public ProductoBatchException(string message, int totalRegistros, int cantidadErrores)
        : base(message, "BATCH_ERROR")
    {
        TotalRegistros = totalRegistros;
        CantidadErrores = cantidadErrores;
        CantidadExitosos = totalRegistros - cantidadErrores;
    }
}

