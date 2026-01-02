using Shared.Exceptions;

namespace GM.CatalogSync.Application.Exceptions;

/// <summary>
/// Excepción lanzada cuando el IdCarga ya existe en la base de datos.
/// </summary>
public class IdCargaDuplicadoException : BusinessValidationException
{
    public string IdCarga { get; }

    public IdCargaDuplicadoException(string idCarga)
        : base(
            $"El ID de carga '{idCarga}' ya existe en la base de datos",
            new List<ValidationError>
            {
                new ValidationError
                {
                    Field = "idCarga",
                    Message = $"El ID de carga '{idCarga}' ya existe",
                    AttemptedValue = idCarga
                }
            })
    {
        IdCarga = idCarga;
    }
}

/// <summary>
/// Excepción lanzada cuando hay errores de validación en EventoCargaProceso.
/// </summary>
public class CargaArchivoSincValidacionException : BusinessValidationException
{
    public CargaArchivoSincValidacionException(string message)
        : base(message, new List<ValidationError>())
    {
    }

    public CargaArchivoSincValidacionException(string message, List<ValidationError> errors)
        : base(message, errors)
    {
    }
}

