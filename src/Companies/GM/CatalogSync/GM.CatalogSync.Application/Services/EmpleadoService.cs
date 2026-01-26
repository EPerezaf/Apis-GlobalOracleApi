using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace GM.CatalogSync.Application.Services;

public class EmpleadoService : IEmpleadoService
{
    private readonly IEmpleadoRepository _repository;
    private readonly ILogger<EmpleadoService> _logger;
    public EmpleadoService(
        IEmpleadoRepository repository,
        ILogger<EmpleadoService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<(List<EmpleadoRespuestaDto> data, int totalRecords)> ObtenerEmpleadosAsync(
        int? idEmpleado,
        int? dealerId,
        string? curp,
        int? activo,
        int? empresaId,
        int page,
        int pageSize,
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[{CorrelationId}] 🔷 [SERVICE] Iniciando ObtenerEmpleadosAsync - Id Empleado: {Idempleado}, Empresa Id: {Empresaid}, Curp: {Curp}, Página: {Page}/{PageSize}",
                correlationId, idEmpleado.ToString() ?? "Todos", dealerId.ToString() ?? "Todas", curp ?? "Todos", page, pageSize);

            // Validar parámetros de paginación
            if (page < 1)
                throw new EmpleadoValidacionException("El número de página debe ser mayor o igual a 1",
                    new List<ValidationError> { new ValidationError { Field = "page", Message = "debe ser >= 1" } });
            
            if (pageSize < 1 || pageSize > 50000)
                throw new EmpleadoValidacionException("El tamaño de página debe estar entre 1 y 50000",
                    new List<ValidationError> { new ValidationError { Field = "pageSize", Message = "debe estar entre 1 y 50000" } });
            
            // Consultar desde Repository
            var (empleados, totalRecords) = await _repository.GetByFilterAsync(
                idEmpleado,dealerId,curp, activo,empresaId, page, pageSize, correlationId);

            var responseDtos = empleados.Select(p => new EmpleadoRespuestaDto
            {
                EmpresaId =p.EmpresaId,
                IdEmpleado = p.IdEmpleado,
                DealerId = p.DealerId,
                Activo = p.Activo,
                Curp = p.Curp,
                NumeroEmpleado = p.NumeroEmpleado,
                Nombre = p.Nombre,
                PrimerApellido = p.PrimerApellido,
                SegundoApellido = p.SegundoApellido,
                Departamento = p.Departamento,
                Puesto = p.Puesto,
                FechaNacimiento = p.FechaNacimiento,
                Edad = p.Edad,
                EmailOrganizacional = p.EmailOrganizacional,
                Telefono = p.Telefono,
                FechaIngreso = p.FechaIngreso,
                JefeNombre = p.JefeNombre,
                JefePrimerApellido = p.JefePrimerApellido,
                JefeSegundoApellido = p.JefeSegundoApellido,
                Antiguedad = p.Antiguedad
            }).ToList();

            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] ✅ [SERVICE] ObtenerEmpleadosAsync completado en {Tiempo}ms - {Count} registros de {Total} totales",
                correlationId, stopwatch.ElapsedMilliseconds, responseDtos.Count, totalRecords);

            return (responseDtos, totalRecords);
        }
        catch (EmpleadoValidacionException)
        {
            throw;
        }
        catch (EmpleadoDataAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{CorrelationId}] ❌ [SERVICE] Error inesperado en ObtenerEmpleadosAsync", correlationId);
            throw new BusinessException("Error al obtener empleados", ex);
        }
    }

    
    
    
}