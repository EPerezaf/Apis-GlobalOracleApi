using GM.CatalogSync.Application.DTOs;
using Shared.Exceptions;

namespace GM.CatalogSync.Application.Exceptions;

public class CatalogoDocNotFoundException : NotFoundException
{
    public CatalogoDocNotFoundException(int id)
        : base($"No se encontró el catálogo de documentos expedientes con ID: {id}", "CatalogoDocExpediente", id.ToString())
    {
    }
}

public class CatalogoDocDuplicadoException : BusinessException
{
    public int CantidadDuplicados { get; set; }
    public List<CatalogoDocResponseDto> RegistrosDuplicados { get; set; }

    public CatalogoDocDuplicadoException(int cantidadDuplicados)
        : base($"Se encontraron {cantidadDuplicados} documentos duplicados en el lote", "CATALOGO_DOC_DUPLICADO")
    {
        CantidadDuplicados = cantidadDuplicados;
        RegistrosDuplicados = new List<CatalogoDocResponseDto>();
    }

    public CatalogoDocDuplicadoException(string message, int cantidadDuplicados, List<CatalogoDocResponseDto>? registrosDuplicados = null)
        : base(message, "CATALOGO_DOC_DUPLICADO")
    {
        CantidadDuplicados = cantidadDuplicados;
        RegistrosDuplicados = registrosDuplicados ?? new List<CatalogoDocResponseDto>();
    }
}

public class CatalogoDocDataAccessException : DataAccessException
{
    public CatalogoDocDataAccessException(string message, Exception innerException)
        : base($"Error de acceso a datos en Catálogo de Documentos: {message}", innerException)
    {
    }
}

public class CatalogoDocValidacionException : BusinessValidationException
{
    public CatalogoDocValidacionException(string message, List<ValidationError> errors)
        : base(message, errors)
    {
    }
}

public class CatalogoDocBatchException : BusinessException
{
    public int TotalRegistros { get; set; }
    public int CantidadErrores { get; set; }
    public int CantidadExitosos { get; set; }

    public CatalogoDocBatchException(string message, int totalRegistros, int cantidadErrores)
        : base(message, "CATALOGO_DOC_BATCH_ERROR")
    {
        TotalRegistros = totalRegistros;
        CantidadErrores = cantidadErrores;
        CantidadExitosos = totalRegistros - cantidadErrores;
    }
}