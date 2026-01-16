using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace GM.CatalogSync.Application.Services;

public class DetalleService : IDetalleService
{
    private readonly IDetalleRepository _repository;
    private readonly ILogger<DetalleService> _logger;

    public DetalleService(
        IDetalleRepository repository,
        ILogger<DetalleService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    

    public async Task<(List<DetalleDealerRespuestaDto> data, int totalRecords)> ObtenerDelearAsync(
        string? dealerId,
        string? nombre,
        string? razonSocial,
        string? rfc,
        int page,
        int pageSize,
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation(
                "[{CorrelationId}] [SERVICE] Iniciando ObtenerDealersAsync - DealerId: {DealerId}, Nombre: {Nombre}, Razon Socila: {Razon Social}, RFC: {Rfc}, Pagina: {Page}/{PageSize}",
                correlationId, dealerId ?? "Todos", nombre ?? "Todos", razonSocial ?? "Todos", rfc ?? "Todos", page, pageSize);

                //VALIDAR PARAMETROS DE PAGINACION
                if(page <1)
                    throw new DetalleDealerValidacionException("El numero de pagina deber ser mayor o igual 1 ",
                        new List<ValidationError> { new ValidationError { Field = "page", Message ="debe ser >= 1"}});
                
                if(pageSize <1 || pageSize > 50000)
                    throw new DetalleDealerValidacionException("El tamaño de pagina deber estar entre 1 y 50000",
                        new List<ValidationError> {new ValidationError { Field ="pageSize", Message= "debe estar enre 1 y 50000"}});

                //Consultar desde el repository 
                var (dealers, totalRecords) = await _repository.GetByFilterAsync(
                    dealerId, nombre, razonSocial, rfc, page, pageSize, correlationId,currentUser);
                

                //Maper Dtos
                var responseDtos = dealers.Select(p => new DetalleDealerRespuestaDto
                {
                    DealerId = p.DealerId,
                    Nombre = p.Nombre,
                    RazonSocial = p.RazonSocial,
                    Rfc = p.Rfc,
                    Tipo = p.Tipo,
                    EmpresaId = p.EmpresaId,
                }).ToList();

                stopwatch.Stop();
                _logger.LogInformation("[{CorrelationId}] [SERVICE] ObtenerDealerAsync compleatdo en {Tiempo}ms - {Count} registros de {Total} totales",
                correlationId, stopwatch.ElapsedMilliseconds, responseDtos.Count, totalRecords);

                return (responseDtos, totalRecords);
        }catch (DetalleDealerValidacionException)
        {
            throw;
        }
        catch (DetalleDealerDataAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{CorrelationId}] [SERVICE] Error inesperado en ObtenerDealersAsync", correlationId);
            throw new BusinessException("Error inesperado al obtener dealers",ex);
        }
    }
    
}