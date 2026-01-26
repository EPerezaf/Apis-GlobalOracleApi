using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace GM.CatalogSync.Application.Services;

public class EmpleadosExpedienteService : IEmpleadoExpedienteService
{
    private readonly IEmpleadoExpedienteRepository _repository;
    private readonly ILogger<EmpleadosExpedienteService> _logger;
    public EmpleadosExpedienteService(
        IEmpleadoExpedienteRepository repository,
        ILogger<EmpleadosExpedienteService> logger)
    {
        _repository = repository;
        _logger = logger;    
    }

    public async Task<(List<EmpleadosExpedienteRespuestaDto> data, int totalRecords)> ObtenerEmpleadosExpedienteAsync(
        int? idDocumento,
        int? idEmpleado,
        int? claveTipoDocumento,
        int? empresaId,
        int page,
        int pageSize,
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[{CorrelationId}] [SERVICE] Iniciando ObtenerEmpleadosExpedienteAsync - Id Documento: {IdDocumento}, Id Empleado: {IdEmpleado}, Clave Tipo Documento: {ClaveTipoDocumento}, Empresa Id: {EmpresaId}, Pagina: {Page}/{PageSize}",
            correlationId, 
            idDocumento.ToString() ?? "Todos",
            idEmpleado.ToString() ?? "Todos",
            claveTipoDocumento.ToString() ?? "Todos",
            empresaId.ToString() ?? "Todos",
            page, pageSize);

            if(page < 1)
                throw new EmpleadoValidacionException("El numero de pagina debe ser mayor o igual a 1",
                    new List<ValidationError> { new ValidationError { Field = "page", Message = "debe ser >= 1"}});

            if(pageSize < 1 || pageSize > 50000)
                throw new EmpleadoValidacionException("El tamaño de pagina debe estar entre 1 y 50000",
                    new List<ValidationError> { new ValidationError { Field = "pageSize", Message ="debe estar entre 1 y 50000"}});
            
            var (empleadosExpediente, totalRecords) = await _repository.GetByFilterAsync(
                idDocumento,
                idEmpleado,
                claveTipoDocumento,
                empresaId, 
                page,
                pageSize,
                correlationId,
                currentUser);
            var responseDtos = empleadosExpediente.Select(p => new EmpleadosExpedienteRespuestaDto
            {
                EmpresaId = p.EmpresaId,
                IdDocumento = p.IdDocumento,
                IdEmpleado = p.IdEmpleado,
                NombreCompleto = p.NombreCompleto,
                NumeroEmpleado  = p.NumeroEmpleado,
                ClaveTipoDocumento = p.ClaveTipoDocumento,
                NombreTipoDocumento = p.NombreTipoDocumento,
                NombreDocumento = p.NombreDocumento,
                Obligatorio = p.Obligatorio,
                ContainerStorage = p.ContainerStorage,
                VersionDocumento = p.VersionDocumento,
                EsVigente = p.EsVigente,
                FechaCarga = p.FechaCarga,
                FechaDocumento = p.FechaDocumento,
                FechaVencimiento = p.FechaVencimiento,
                Observaciones = p.Observaciones
                /*IdDocumento = p.IdDocumento,
                IdEmpleado = p.IdEmpleado,
                ClaveTipoDocumento = p.ClaveTipoDocumento,
                NombreDocumento = p.NombreDocumento,
                NombreArchivoStorage = p.NombreArchivoStorage,
                RutaStorage = p.RutaStorage,
                ContainerStorage = p.ContainerStorage,
                VersionDocumento = p.VersionDocumento,
                EsVigente = p.EsVigente,
                FechaCarga = p.FechaCarga,
                FechaDocumento = p.FechaDocumento,
                FechaVencimiento = p.FechaVencimiento,
                Observaciones = p.Observaciones,
                UsuarioAlta = p.UsuarioAlta,
                FechaAlta = p.FechaAlta,
                UsuarioModificacion = p.UsuarioModificacion,
                FechaModificacion = p.FechaModificacion*/
            }).ToList();

            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] [SERVICE] Completado ObtenerEmpleadosExpedienteAsync  en Tiempo: {Tiempo}ms de {Count} de Total: {Total}",
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
        catch (Exception)
        {
            stopwatch.Stop();
            _logger.LogError("[{CorrelationId}] [SERVICE] Error inesperado en ObtenerEmpleadosExpedineteAsync",correlationId);
            throw new BusinessException("Error al obtener empleados por expediente");
        }
    }
    
    
}