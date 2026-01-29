using System.ComponentModel;
using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace GM.CatalogSync.Application.Services;

public class CatalogoDocExpedienteService : ICatalogoDocExpedienteService
{
    private readonly ICatalogoDocExpedienteRepository _repository;
    private readonly ILogger<CatalogoDocExpedienteService> _logger;
    public CatalogoDocExpedienteService(
        ICatalogoDocExpedienteRepository repository,
        ILogger<CatalogoDocExpedienteService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<(List<CatalogoDocResponseDto> data, int totalRecords)> ObtenerCatalogoAsync(
        int? claveTipoDocumento,
        int? idEmpleado,
        int? empresaId,
        int? idDocumento,
        string? estatusExpediente,
        int page,
        int pageSize,
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[{CorrelationId}] [SERVICE] Iniciando ObtenerCatalogoAsync - Id Documento: {IdDocumento}, Id Empleado: {IdEmpleado}, Clave Tipo Documento: {ClaveTipoDocumento}, Empresa Id: {EmpresaId}, Pagina: {Page}/{PageSize}",
            correlationId,
            idDocumento.ToString() ?? "Todos",
            idEmpleado.ToString() ?? "Todos",
            claveTipoDocumento.ToString() ?? "Todos",
            empresaId.ToString() ?? "Todos",
            page, pageSize);

            if (page < 1)
                throw new EmpleadoValidacionException("El numero de pagina debe ser mayor o igual a 1",
                    new List<ValidationError> { new ValidationError { Field = "page", Message = "debe ser >= 1" } });

            if (pageSize < 1 || pageSize > 50000)
                throw new EmpleadoValidacionException("El tamaño de pagina debe estar entre 1 y 50000",
                    new List<ValidationError> { new ValidationError { Field = "pageSize", Message = "debe estar entre 1 y 50000" } });

            var (catalogoDoc, totalRecords) = await _repository.GetByFilterAsync(
                 claveTipoDocumento,
             idEmpleado,
             empresaId,
             idDocumento,
             estatusExpediente,
             currentUser,
             correlationId,
            page,
             pageSize);
            var responseDtos = catalogoDoc.Select(p => new CatalogoDocResponseDto
            {
                EmpresaId = p.EmpresaId,
                IdEmpleado = p.IdEmpleado,
                ClaveTipoDocumento = p.ClaveTipoDocumento,
                NombreTipoDocumento = p.NombreTipoDocumento,
                Obligatorio = p.Obligatorio,
                IdDocumento = p.IdDocumento,
                NombreArchivoStorage = p.NombreArchivoStorage,
                ContainerStorage = p.ContainerStorage,
                RutaStorage = p.RutaStorage,
                Observaciones = p.Observaciones,
                FechaCarga = p.FechaCarga,
                FechaVencimiento = p.FechaVencimiento,
                EsVigente = p.EsVigente,
                EstatusExpediente = p.EstatusExpediente,
                ExisteArchivo = p.ExisteArchivo
            }).ToList();

            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] [SERVICE] Completado ObtenerCatalogoAsync en Tiempo: {Tiempo}ms de {Count} de Total: {Total}",
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
            _logger.LogError("[{CorrelationId}] [SERVICE] Error inesperado en ObtenerCatalogoAsync", correlationId);
            throw new BusinessException("Error al obtener catalogo de documentos");
        }
    }


}