using GM.CatalogSync.Application.DTOs;
using Shared.Exceptions;

namespace GM.CatalogSync.Application.Exceptions;

public class EmpleadoNotFoundException : NotFoundException
{
    public EmpleadoNotFoundException(int idEmpleado)
        :base($"No se encontro el empleado con ID:{idEmpleado}", "Empleado", idEmpleado.ToString())
    {
        
    }
}

public class EmpleadoDuplicadoException : BusinessException
{
    public int CantidadDuplicados { get; set; }
    public List<EmpleadoCrearDto> RegistrosDuplicados { get; set; }

    public EmpleadoDuplicadoException(int cantidadDuplicados)
        : base($"Se encontraron {cantidadDuplicados} empleados duplicados en el lote", "EMPLEADO_DUPLICADO")
    {
        CantidadDuplicados = cantidadDuplicados;
        RegistrosDuplicados = new List<EmpleadoCrearDto>();
    }

    public EmpleadoDuplicadoException(string message, int cantidadDuplicados, List<EmpleadoCrearDto>? registrosDuplicados = null)
        : base(message, "EMPLEADO_DUPLICADO")
    {
        CantidadDuplicados = cantidadDuplicados;
        RegistrosDuplicados = registrosDuplicados ?? new List<EmpleadoCrearDto>();
    }
}

public class EmpleadoDataAccessException : DataAccessException
{
    public EmpleadoDataAccessException(string message, Exception innerException)
        : base($"Error de acceso a datos en Empleado: {message}", innerException)
    {
    }
}

public class EmpleadoValidacionException : BusinessValidationException
{
    public EmpleadoValidacionException(string message, List<ValidationError> errors)
        : base(message, errors)
    {
    }
}

public class EmpleadoBatchException : BusinessException
{
    public int TotalRegistros { get; set; }
    public int CantidadErrores { get; set; }
    public int CantidadExitosos { get; set; }

    public EmpleadoBatchException(string message, int totalRegistros, int cantidadErrores)
        : base(message, "BATCH_ERROR")
    {
        TotalRegistros = totalRegistros;
        CantidadErrores = cantidadErrores;
        CantidadExitosos = totalRegistros - cantidadErrores;
    }
}