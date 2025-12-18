namespace Shared.Exceptions
{
    /// <summary>
    /// Excepción para errores de acceso a datos
    /// </summary>
    public class DataAccessException : Exception
    {
        public string? SqlQuery { get; set; }

        public DataAccessException(string message)
            : base(message)
        {
        }

        public DataAccessException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public DataAccessException(string message, Exception innerException, string sqlQuery)
            : base(message, innerException)
        {
            SqlQuery = sqlQuery;
        }
    }
}

