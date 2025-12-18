namespace Shared.Exceptions
{
    /// <summary>
    /// Excepción base para errores de negocio
    /// </summary>
    public class BusinessException : Exception
    {
        public string ErrorCode { get; set; }

        public BusinessException(string message, string errorCode = "BUSINESS_ERROR")
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public BusinessException(string message, Exception innerException, string errorCode = "BUSINESS_ERROR")
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}

