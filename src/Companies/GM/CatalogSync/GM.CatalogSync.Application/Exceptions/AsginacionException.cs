using GM.CatalogSync.Application.DTOs;
using Shared.Exceptions;

namespace GM.CatalogSync.Application.Exceptions;

public class AsignacionNotFoundException : NotFoundException
{
    public AsignacionNotFoundException(int asignacionId)
        : base ($"No se encontro la asignacion con ID: {asignacionId}", "Asignacion", asignacionId.ToString())
    {
        
    }
}

public class AsignacionDuplicadoException : BusinessException
{
    public int CantidadDuplicados { get; set; }
    public List<AsignacionCrearDto> RegistrosDuplicados {get; set; }

    public AsignacionDuplicadoException(int cantidadDuplicados)
        : base($"Se encontraron {cantidadDuplicados} asignaciones duplicados en el lote", "ASIGNACION_DUPLICADO")
    {
        CantidadDuplicados = cantidadDuplicados;
        RegistrosDuplicados = new List<AsignacionCrearDto>();
    }

    public AsignacionDuplicadoException(string message, int cantidadDuplicados, List<AsignacionCrearDto>? registrosDuplicados = null)
        : base(message, "ASGINACION_DUPLICADO")
    {
        CantidadDuplicados = cantidadDuplicados;
        RegistrosDuplicados = registrosDuplicados ?? new List<AsignacionCrearDto>();
    }
}

public class AsignacionDataAccessException : DataAccessException
{
    public AsignacionDataAccessException(string message, Exception innerException)
        : base($"Error de acceso a datos en Asignacion: { message}", innerException)
    {
        
    }
}

public class AsignacionValidacionException : BusinessValidationException
{
    public AsignacionValidacionException(string message, List<ValidationError> errors)
        : base(message, errors)
    {
        
    }
}

public class AsignacionBatchException : BusinessException
{
    public int TotalRegistros { get; set; }
    public int CantidadErrores { get; set;}
    public int CantidadExitosos { get; set;}

    public AsignacionBatchException(string message, int totalRegistros, int cantidadErrores)
        : base (message, "BATCH_ERROR")
    {
        TotalRegistros = totalRegistros;
        CantidadErrores = cantidadErrores;
        CantidadExitosos = totalRegistros - cantidadErrores;
    }
}