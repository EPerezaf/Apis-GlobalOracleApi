using Shared.Exceptions;
using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Exceptions
{
    public class ProductNotFoundException : NotFoundException
    {
        public ProductNotFoundException(int productoId)
            : base($"No se encontró el producto con ID: {productoId}", "Product", productoId.ToString())
        {
        }
    }

    public class ProductDuplicateException : BusinessException
    {
        public int DuplicateCount { get; set; }
        public List<ProductCreateDto> DuplicateRecords { get; set; }

        public ProductDuplicateException(int duplicateCount)
            : base($"Se encontraron {duplicateCount} productos duplicados en el lote", "DUPLICATE_PRODUCT")
        {
            DuplicateCount = duplicateCount;
            DuplicateRecords = new List<ProductCreateDto>();
        }

        public ProductDuplicateException(string message, int duplicateCount, List<ProductCreateDto>? duplicateRecords = null)
            : base(message, "DUPLICATE_PRODUCT")
        {
            DuplicateCount = duplicateCount;
            DuplicateRecords = duplicateRecords ?? new List<ProductCreateDto>();
        }
    }

    public class ProductDataAccessException : DataAccessException
    {
        public ProductDataAccessException(string message, Exception innerException)
            : base($"Error de acceso a datos en Product: {message}", innerException)
        {
        }
    }

    public class ProductValidationException : BusinessValidationException
    {
        public ProductValidationException(string message, List<ValidationError> errors)
            : base(message, errors)
        {
        }
    }

    public class ProductBatchException : BusinessException
    {
        public int TotalRecords { get; set; }
        public int ErrorCount { get; set; }
        public int SuccessCount { get; set; }

        public ProductBatchException(string message, int totalRecords, int errorCount)
            : base(message, "BATCH_ERROR")
        {
            TotalRecords = totalRecords;
            ErrorCount = errorCount;
            SuccessCount = totalRecords - errorCount;
        }
    }
}

