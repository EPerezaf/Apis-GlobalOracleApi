using GM.CatalogSync.Application.DTOs;
using Shared.Exceptions;

namespace GM.CatalogSync.Application.Exceptions;

public class CampañaNotFoundException : NotFoundException
{
    public CampañaNotFoundException(int campañaId)
        : base($"No se encontró la campaña con ID: {campañaId}", "Campaña", campañaId.ToString())
    {
        
    }
}

public class CampañaDuplicadoException : BusinessException
{
    public int CantidadDuplicados { get; set;}
    public List<CampañaCrearDto> RegistrosDuplicados {get; set;}

    public CampañaDuplicadoException(int cantidadDuplicados)
        :base($"Se encontraron {cantidadDuplicados} campañas duplicadas en el lote", "CAMPAÑA_DUPLICADO")
    {
        CantidadDuplicados = cantidadDuplicados;
        RegistrosDuplicados = new List<CampañaCrearDto>();
    }

    public CampañaDuplicadoException(string message, int cantidadDuplicados, List<CampañaCrearDto>? registrosDuplicados = null)
        :base(message, "CAMPAÑA_DUPLICADA")
    {
        CantidadDuplicados = cantidadDuplicados;
        RegistrosDuplicados = registrosDuplicados ?? new List<CampañaCrearDto>();
    }
}

public class CampañaDataAccessException : DataAccessException
{
    public CampañaDataAccessException(string message, Exception innerException)
        :base($"Error de acceso a datos en Campaña: {message}", innerException)
    {
        
    }
}

public class CampañaValidacionException : BusinessValidationException
{
    public CampañaValidacionException(string message, List<ValidationError> errors)
        : base(message, errors)
    {
        
    }
}

public class CampañaBatchException : BusinessException
{
    public int TotalRegistros { get;set;}
    public int CantidadErrores {get; set;}
    public int CantidadExitosos {get;set;}
    public CampañaBatchException(string message, int totalRegistros, int cantidadErrores)
        :base(message, "BATCH_ERROR")
    {
        TotalRegistros =totalRegistros;
        CantidadErrores = cantidadErrores;
        CantidadExitosos = totalRegistros - cantidadErrores;
    }
}