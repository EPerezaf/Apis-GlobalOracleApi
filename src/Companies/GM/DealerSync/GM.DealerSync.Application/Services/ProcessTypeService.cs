using GM.DealerSync.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GM.DealerSync.Application.Services;

/// <summary>
/// Implementación del servicio para validación y gestión de tipos de procesos implementados
/// </summary>
/// <remarks>
/// Este servicio lee los procesos implementados desde appsettings.json.
/// 
/// Para agregar un nuevo proceso:
/// 1. Agregar el enum value en ProcessType
/// 2. Implementar la lógica del proceso en BatchSyncJobService o donde corresponda
/// 3. Agregar el nombre del proceso a "BatchSync:ProcesosImplementados" en appsettings.json
/// 
/// En el futuro, esto se migrará a base de datos para gestión dinámica.
/// </remarks>
public class ProcessTypeService : IProcessTypeService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProcessTypeService> _logger;
    private readonly HashSet<string> _procesosImplementados;

    public ProcessTypeService(IConfiguration configuration, ILogger<ProcessTypeService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Leer procesos implementados desde appsettings.json
        var procesosConfig = _configuration.GetSection("BatchSync:ProcesosImplementados").Get<List<string>>();
        
        if (procesosConfig != null && procesosConfig.Any())
        {
            _procesosImplementados = new HashSet<string>(procesosConfig, StringComparer.OrdinalIgnoreCase);
            _logger.LogInformation(
                "✅ ProcessTypeService: {Count} proceso(s) implementado(s) cargado(s) desde appsettings.json: {Procesos}",
                _procesosImplementados.Count,
                string.Join(", ", _procesosImplementados.OrderBy(p => p)));
        }
        else
        {
            // Fallback a valores por defecto si no hay configuración
            _procesosImplementados = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                nameof(ProcessType.ProductList)
            };
            _logger.LogWarning(
                "⚠️ ProcessTypeService: No se encontró configuración 'BatchSync:ProcesosImplementados' en appsettings.json. " +
                "Usando valores por defecto: {Procesos}",
                string.Join(", ", _procesosImplementados));
        }
    }

    /// <inheritdoc />
    public bool IsProcessTypeImplemented(string processType)
    {
        if (string.IsNullOrWhiteSpace(processType))
        {
            return false;
        }

        // Verificar si está en la lista de implementados (leída desde appsettings.json)
        return _procesosImplementados.Contains(processType);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetImplementedProcessTypes()
    {
        return _procesosImplementados.OrderBy(p => p);
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllAvailableProcessTypes()
    {
        // Retornar todos los valores del enum (disponibles pero pueden no estar implementados)
        return Enum.GetNames(typeof(ProcessType)).OrderBy(p => p);
    }
}

