namespace Shared.Exceptions
{
    /// <summary>
    /// Excepción para errores de validación de negocio
    /// </summary>
    public class BusinessValidationException : BusinessException
    {
        public List<ValidationError> Errors { get; set; }

        public BusinessValidationException(string message, List<ValidationError> errors)
            : base(message, "VALIDATION_ERROR")
        {
            Errors = errors ?? new List<ValidationError>();
        }
    }

    /// <summary>
    /// Detalle de un error de validación
    /// </summary>
    public class ValidationError
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object? AttemptedValue { get; set; }
    }
}

