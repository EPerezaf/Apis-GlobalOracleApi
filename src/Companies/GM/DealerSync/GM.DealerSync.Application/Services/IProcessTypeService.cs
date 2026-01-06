namespace GM.DealerSync.Application.Services;

/// <summary>
/// Servicio para validación y gestión de tipos de procesos implementados
/// </summary>
public interface IProcessTypeService
{
    /// <summary>
    /// Verifica si un tipo de proceso está implementado y permitido
    /// </summary>
    /// <param name="processType">Nombre del tipo de proceso</param>
    /// <returns>True si el proceso está implementado, False en caso contrario</returns>
    bool IsProcessTypeImplemented(string processType);

    /// <summary>
    /// Obtiene todos los tipos de procesos implementados y permitidos
    /// </summary>
    /// <returns>Lista de nombres de procesos implementados</returns>
    IEnumerable<string> GetImplementedProcessTypes();

    /// <summary>
    /// Obtiene todos los tipos de procesos disponibles (todos los valores del enum)
    /// </summary>
    /// <returns>Lista de nombres de todos los procesos disponibles</returns>
    IEnumerable<string> GetAllAvailableProcessTypes();
}

