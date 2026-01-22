using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace GM.CatalogSync.Application.Services;

public class EmpleadoHistoricoService : IEmpleadoHistoricoService
{
    private readonly IEmpleadoHistoricoRepository _repository;
    private readonly ILogger<EmpleadoHistoricoService> _logger;
    public EmpleadoHistoricoService(
        IEmpleadoHistoricoRepository repository,
        ILogger<EmpleadoHistoricoService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    public async Task<(List<EmpleadoHistoricoRespuestaDto> data, int totalRecords)> ObtenerEmpleadosHistoricosAsync(
        int? idAsignacion,
        int? idEmpleado,
        int? dealerId,
        string? clavePuesto,
        string? departamento,
        int? esActual,
        int? empresaId,
        int page,
        int pageSize,
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[{CorrelationId}] 🔷 [SERVICE] Iniciando ObtenerEmpleadosHistoricosAsync - Id Asignacion: {IdAsignacion}, Id Empleado: {IdEmpleado}, Dealer Id: {DealerId}, Clave Puesto: {ClavePuesto}, Departamento: {Departamento}, Es Actual: {EsActual}, Empresa Id: {EmpresaId}, Página: {Page}/{PageSize}",
                correlationId, 
                idAsignacion.ToString() ?? "Todos", 
                idEmpleado.ToString() ?? "Todos", 
                dealerId.ToString() ?? "Todas", 
                clavePuesto ?? "Todos", 
                departamento ?? "Todos",
                esActual.ToString() ?? "Todos",
                empresaId.ToString() ?? "Todos",
                page, pageSize);

            // Validar parámetros de paginación
            if (page < 1)
                throw new EmpleadoValidacionException("El número de página debe ser mayor o igual a 1",
                    new List<ValidationError> { new ValidationError { Field = "page", Message = "debe ser >= 1" } });
            
            if (pageSize < 1 || pageSize > 50000)
                throw new EmpleadoValidacionException("El tamaño de página debe estar entre 1 y 50000",
                    new List<ValidationError> { new ValidationError { Field = "pageSize", Message = "debe estar entre 1 y 50000" } });
            
            // Consultar desde Repository
            var (empleadosHistoricos, totalRecords) = await _repository.GetByFilterAsync(
                idAsignacion,idEmpleado,dealerId,clavePuesto,departamento,esActual,empresaId, page, pageSize, currentUser, correlationId);

            var responseDtos = empleadosHistoricos.Select(p => new EmpleadoHistoricoRespuestaDto
            {
                IdAsignacionPuesto = p.IdAsignacionPuesto,
                IdEmpleado = p.IdEmpleado,
                DealerId = p.DealerId,
                ClavePuesto = p.ClavePuesto,
                NombrePuesto = p.NombrePuesto,
                Departamento = p.Departamento,
                IdEmpleadoJefe = p.IdEmpleadoJefe,
                FechaInicioAsignacion = p.FechaInicioAsignacion,
                FechaFinAsignacion = p.FechaFinAsignacion,
                EsActual = p.EsActual,
                MotivoCambio = p.MotivoCambio,
                Observaciones = p.Observaciones,
                UsuarioAlta = p.UsuarioAlta,
                FechaAlta = p.FechaAlta,
                UsuarioModifica = p.UsuarioModifica,
                FechaModifica = p.FechaModifica
            }).ToList();

            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] ✅ [SERVICE] Finalizado ObtenerEmpleadosHistoricosAsync completo en {Tiempo}ms - {Count} registros de {Total} totales ",
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
            _logger.LogError(ex, "[{CorrelationId}] [SERVICE] Error inesperado en ObtenerEmpleadosAsync", correlationId);
            throw new BusinessException("Error al obtener empleados historicos", ex);
        }
    }
}