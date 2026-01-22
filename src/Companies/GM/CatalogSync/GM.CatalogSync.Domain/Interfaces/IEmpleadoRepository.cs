using GM.CatalogSync.Domain.Entities;

namespace GM.CatalogSync.Domain.Interfaces;

public interface IEmpleadoRepository
{
    Task<(List<Empleado> empleados, int totalRecords)> GetByFilterAsync(
        int? idEmpleado,
        int? dealerId,
        string? curp,
        string? numeroEmpleado,
        int? empresaId,
        int page,
        int pageSize,
        string correlationId);

    Task<int> GetTotalCountAsync(
        int? idEmpleado,
        int? dealerId,
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