using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Logging;

namespace Shared.Infrastructure
{
    /// <summary>
    /// Factory para crear conexiones Oracle de manera segura
    /// </summary>
    public interface IOracleConnectionFactory
    {
        Task<OracleConnection> CreateConnectionAsync();
    }

    /// <summary>
    /// Implementación de factory para conexiones Oracle
    /// </summary>
    public class OracleConnectionFactory : IOracleConnectionFactory
    {
        private readonly string _connectionString;
        private readonly ILogger<OracleConnectionFactory>? _logger;

        public OracleConnectionFactory(string connectionString, ILogger<OracleConnectionFactory>? logger = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger;
        }

        public async Task<OracleConnection> CreateConnectionAsync()
        {
            try
            {
                var connection = new OracleConnection(_connectionString);
                await connection.OpenAsync();
                _logger?.LogDebug("✅ Conexión Oracle abierta exitosamente");
                return connection;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Error al abrir conexión Oracle");
                throw;
            }
        }
    }
}

