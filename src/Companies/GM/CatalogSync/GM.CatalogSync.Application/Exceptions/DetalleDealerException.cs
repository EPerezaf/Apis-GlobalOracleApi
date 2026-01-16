using GM.CatalogSync.Application.DTOs;
using Shared.Exceptions;

namespace GM.CatalogSync.Application.Exceptions;

public class DetalleDealerNotFoundException : NotFoundException
{
    public DetalleDealerNotFoundException(int dealerId)
        : base($"No se encontro el dealer con ID: {dealerId}", "Dealer", dealerId.ToString())
    {
        
    }
}

public class DetalleDealerDuplicadoException : BusinessException
{
    public int CantidadDuplicados { get; set;}
    public List<DetalleCrearDto> RegistrosDuplicados { get; set;}

    public DetalleDealerDuplicadoException(int cantidadDuplicados)
        : base($"Se encontraron {cantidadDuplicados} dealers duplicados en el lote", "DEALER_DUPLICADO")
    {
        CantidadDuplicados = cantidadDuplicados;
        RegistrosDuplicados = new List<DetalleCrearDto>();
    }

    public DetalleDealerDuplicadoException(string message, int cantidadDuplicados, List<DetalleCrearDto> ? registrosDuplicados = null)
        : base(message, "DETALLEDEALER_DUPLICADO")
    {
        CantidadDuplicados = cantidadDuplicados;
        RegistrosDuplicados = registrosDuplicados ?? new List<DetalleCrearDto>();
    }
}

public class DetalleDealerDataAccessException : DataAccessException
{
    public DetalleDealerDataAccessException(string message, Exception innerException)
        : base($"Error de acceso a datos en Detalle Dealer: {message}", innerException)
    {
        
    }
}

public class DetalleDealerValidacionException : BusinessValidationException
{
    public DetalleDealerValidacionException(string message, List<ValidationError> errors)
        : base(message, errors)
    {
        
    }
}

public class DetalleDealerBatchException : BusinessException
{
    public int TotalRegistros { get; set;}
    public int CantidadErrores { get; set;}
    public int CantidadExitosos { get; set;}

    public DetalleDealerBatchException(string message, int totalRegistros, int cantidadErrores)
        : base (message, "BATCH_ERROR")
    {
        TotalRegistros = totalRegistros;
        CantidadErrores = cantidadErrores;
        CantidadExitosos = totalRegistros - cantidadErrores;
    }
}