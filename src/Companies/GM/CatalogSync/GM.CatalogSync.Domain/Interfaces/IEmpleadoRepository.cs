using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

public interface IEmpleadoRepository
{
    Task<(List<Empleado> empleados, int totalRecords)> GetByFilterAsync(
        int? idEmpleado,
        string? dealerId,
        string? curp,
        int? activo,
        int? empresaId,
        int page,
        int pageSize,
        string correlationId);

    Task<int> GetTotalCountAsync(
        int? idEmpleado,
        string? dealerId,
        string? curp,
        string? numeroEmpleado,
        string correlationId);
    
    /*Task<int>InsertAsync(
        Empleado empleado,
        string correlationId,
        string currentUser);
    
    Task<int> DeleteAsync(
        string currentUser,
        string correlationId);*/
    
    
}