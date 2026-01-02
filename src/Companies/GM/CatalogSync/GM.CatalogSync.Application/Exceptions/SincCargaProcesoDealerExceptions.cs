using Shared.Exceptions;

namespace GM.CatalogSync.Application.Exceptions;

/// <summary>
/// Excepción lanzada cuando hay errores de validación en SincCargaProcesoDealer.
/// </summary>
public class SincArchivoDealerValidacionException : BusinessValidationException
{
    public SincArchivoDealerValidacionException(string message)
        : base(message, new List<ValidationError>())
    {
    }

    public SincArchivoDealerValidacionException(string message, List<ValidationError> errors)
        : base(message, errors)
    {
    }
}

/// <summary>
/// Excepción lanzada cuando ya existe un registro de sincronización duplicado.
/// </summary>
public class SincArchivoDealerDuplicadoException : BusinessValidationException
{
    public string Proceso { get; }
    public int EventoCargaProcesoId { get; }
    public string DealerBac { get; }
    public DateTime FechaSincronizacionPrevia { get; }

    public SincArchivoDealerDuplicadoException(
        string proceso,
        int eventoCargaProcesoId,
        string dealerBac,
        DateTime fechaSincronizacionPrevia)
        : base(
            $"Ya existe un registro para el proceso '{proceso}', eventoCargaProcesoId '{eventoCargaProcesoId}' y dealer '{dealerBac}'",
            new List<ValidationError>
            {
                new ValidationError
                {
                    Field = "(proceso, eventoCargaProcesoId, dealerBac)",
                    Message = $"Ya existe un registro para el proceso '{proceso}', eventoCargaProcesoId '{eventoCargaProcesoId}' y dealer '{dealerBac}'. Fecha de sincronización previa: {fechaSincronizacionPrevia:dd/MM/yyyy HH:mm:ss}",
                    AttemptedValue = $"{proceso}|{eventoCargaProcesoId}|{dealerBac}"
                }
            })
    {
        Proceso = proceso;
        EventoCargaProcesoId = eventoCargaProcesoId;
        DealerBac = dealerBac;
        FechaSincronizacionPrevia = fechaSincronizacionPrevia;
    }
}

