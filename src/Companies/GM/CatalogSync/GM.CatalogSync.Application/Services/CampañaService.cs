using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace GM.CatalogSync.Application.Services;

public class CampañaService: ICampañaService
{
    private readonly ICampañaRepository _repository;
    private readonly ILogger<CampañaService> _logger;

    public CampañaService( 
        ICampañaRepository repository,
        ILogger<CampañaService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<(List<CampañaRespuestDto> data, int totalRecords)> ObtenerCampañasAsync(
        string? sourceCodeId,
        string? id,
        string? name,
        int page,
        int pageSize,
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[{CorrelationId}] [SERVICE] Iniciando ObtenerCampañasAsync - SourceCodeId: {SourceCodeId}, Id: {Id}, Name: {Name}, Pagina: {Page}/{PageSize}",
            correlationId, sourceCodeId ?? "Todos", id ?? "Todos", name ?? "Todos", page, pageSize);

            //VALIDAR PARAMETROS DE PAGINACION
            if(page < 1)
            {
                throw new CampañaValidacionException("El numero de pagina debe ser mayor o igual a 1",
                 new List<ValidationError> { new ValidationError { Field = "page", Message = "debe ser >= 1"}});
                 
            }
            if (pageSize < 1 || pageSize > 50000)
            {
                throw new CampañaValidacionException("El tamaño de pagina debe ser mayor a 1 o menor de 50000",
                    new List<ValidationError> { new ValidationError { Field = "page", Message = "debe esatr entre 1 y 50000"}});
            }

            //CONSULTAR DESDE REPOSITORY
            var (campañas, totalRecords) = await _repository.GetByFilterAsync(
                sourceCodeId, id, name, page, pageSize, correlationId);
            
            //MAPEAR A DTOS
            var responseDtos = campañas.Select(p => new CampañaRespuestDto
            {
                CampañaId = p.CampañaId,
                SourceCodeId = p.SourceCodeId,
                Id = p.Id,
                Name = p.Name,
                RecordTypeId = p.RecordTypeId,
                LeadRecordType = p.LeadRecordType,
                LeadEnquiryType = p.LeadEnquiryType,
                LeadSource = p.LeadSource,
                LeadSourceDetails = p.LeadSourceDetails,
                Status = p.Status,
                FechaAlta = p.FechaAlta,
                UsuarioAlta = p.UsuarioAlta,
                FechaModificacion = p.FechaModificacion,
                UsuarioModificacion = p.UsuarioModificacion
            }).ToList();

            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] [SERVICE] ObtenerCampañasAsync completado en {Tiempo}ms - {Count} registros de {Total} totales",
            correlationId, stopwatch.ElapsedMilliseconds, responseDtos.Count, totalRecords);

            return (responseDtos, totalRecords);
        }
        catch (CampañaValidacionException)
        {
            throw;
        }
        catch (CampañaDataAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{CorrelationId}] [SERVICE] Error inesperado en ObtenerCampañasAsync", correlationId);
            throw new BusinessException("Error al obtener las campañas", ex);
        }
    }

    public async Task<CampañaBatchResultadoDto> ProcesarBatchInsertAsync(
        List<CampañaCrearDto> campañas,
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[{CorrelationId}] [SERVICE] Iniciando ProcesarBatchInsertAsync - Usuario: {User}, Total: {Count}",
            correlationId, currentUser, campañas?.Count ?? 0);

            //VALIDAR BATCH
            if(campañas == null || campañas.Count ==0)
            {
                throw new CampañaValidacionException("La lista de campañas no puede estar vacia",
                    new List<ValidationError> { new ValidationError { Field = "josn", Message = "maximo 10000 registros"}});
            }

            //VALIDAR CAMPOS OBLIGATORIOS
            var erroresValidacion = ValidarRegistrosBatch(campañas, correlationId);
            if (erroresValidacion.Any())
            {
                throw new CampañaValidacionException(
                    $"Se encontraron {erroresValidacion.Count} errores de valdiacion",
                    erroresValidacion);
                
            }

            //DETECTAR DUPLICADOS CONTRA LA BASE DE DATO
            /*var duplicadosEnBD = await ValidarDuplicadosEnBaseDatosAsync(campañas,correlationId);
            if (duplicadosEnBD.Any())
            {
                var mensaje = $"Se encontraron {duplicadosEnBD.Count} registro(s) duplicado(s) que ya existente en la base de datos." +
                $"La combinacion de sourceCodeId, id, name debe ser unica.";
                throw new CampañaDuplicadoException(mensaje, duplicadosEnBD.Count);
            }*/

            //MAPEAR DTOS A ENTITIES
            var campañaEntities = campañas.Select(p => new Campaña
            {
                SourceCodeId = p.SourceCodeId.Trim(),
                Id = p.Id.Trim(),
                Name = p.Name.Trim(),
                RecordTypeId = p.RecordTypeId.Trim(),
                LeadRecordType = p.LeadRecordType.Trim(),
                LeadEnquiryType = string.IsNullOrEmpty(p.LeadEnquiryType) ? null : p.LeadEnquiryType,
                LeadSource = string.IsNullOrWhiteSpace(p.LeadSource) ? null : p.LeadSource,
                LeadSourceDetails = string.IsNullOrWhiteSpace(p.LeadSourceDetails) ? null : p.LeadSourceDetails,
                Status = string.IsNullOrWhiteSpace(p.Status) ? null : p.Status
            }).ToList();

            //EJECUTAR INSERT BATCH
            var registrosInsertados = await _repository.UpsertBatchWithTransactionAsync(
                campañaEntities, currentUser, correlationId);

            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] [SERVICE] ProcesarBatchInsertASync completado en {Teimpo}ms - Insertados: {Ins}",
             correlationId,stopwatch.ElapsedMilliseconds, registrosInsertados);

             return new CampañaBatchResultadoDto
             {
                 RegistrosTotales = campañas.Count,
                 RegistrosInsertados = registrosInsertados,
                 RegistrosActualizados = 0,
                 RegistrosError = campañas.Count - registrosInsertados,
                 OmitidosPorError = 0
             };
        }    
        catch (CampañaDuplicadoException)
        {
            throw;
        }
        catch (CampañaValidacionException)
        {
            throw;
        }
        catch (CampañaDataAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{CorrelationId}] [SERVICE] Error inesperado en ProcesarBatchInsertAsyn", 
            correlationId);
            throw new BusinessException("Error al procesar lote de campañas",ex);
        }
    }

    public async Task<int>EliminarTodosAsync(
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[{CorrelationId}] [SERVICE] EliminarTodoASync - Usuario {User}",
            correlationId, currentUser);

            var rowsAffected = await _repository.DeleteAllAsync(currentUser, correlationId);

            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] [SERVICE] EliminarTodosAsync completado en {Tiempo}ms - {Rows} filas eliminadas",
            correlationId, stopwatch.ElapsedMilliseconds, rowsAffected);

            return rowsAffected;
        }    
        catch (CampañaDataAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{CorrelationId}] [SERVICE] Error inesperado en EliminarTodosASync", correlationId);
            throw new BusinessException("Error al eliminar campañas", ex);
        }
    }

    #region Private Helpers

    private List<ValidationError> ValidarRegistrosBatch(List<CampañaCrearDto> campañas, string correlationId)
    {
        var errors = new List<ValidationError>();
        for(int i = 0; i < campañas.Count; i++)
        {
            var campaña = campañas[i];
            var index = i + 1;

            if(string.IsNullOrWhiteSpace(campaña.SourceCodeId))
            errors.Add(new ValidationError { Field = $"json[{index}].sourceCodeId", Message = "es requerido"});

            if(string.IsNullOrWhiteSpace(campaña.Id))
                errors.Add(new ValidationError { Field = $"json[{index}].Id", Message = "es requerido"});

            if(string.IsNullOrWhiteSpace(campaña.Name))
                errors.Add(new ValidationError { Field = $"json[{index}].name", Message = "es requerido"});
            
            if(string.IsNullOrWhiteSpace(campaña.RecordTypeId))
                errors.Add(new ValidationError { Field = $"json.[{index}].recordTypeId", Message = "es requerido"});

            if(string.IsNullOrWhiteSpace(campaña.LeadRecordType))
                errors.Add(new ValidationError { Field = $"json.[{index}].leadRecordType", Message = "es requerido"});
        }
        return errors;
    }

    private List<CampañaCrearDto> DetectarDuplicadosEnBatch(List<CampañaCrearDto> campañas, string correlationId)
    {
        var normalizedRecords = campañas
            .Select((r,idx) => new
            {
                Index = idx,
                OriginalRecord = r,
                SourceCodeIdNormalized = (r.SourceCodeId ?? string.Empty).Trim(),
                Id = r.Id,
                NameNormalized = (r.Name ?? string.Empty).Trim()
            })
            .ToList();

            var duplicadosGrupos = normalizedRecords
                .GroupBy(r => new { r.SourceCodeIdNormalized, r.Id, r.NameNormalized})
                .Where(g => g.Count() > 1)
                .ToList();
            var duplicados = new List<CampañaCrearDto>();

        if (duplicadosGrupos.Any())
        {
            foreach (var grupo in duplicadosGrupos)
            {
                _logger.LogWarning("[{CorrelationId}] [SERVICE] Duplicados encontrados en batch - Campaña: '{Campaña}', Id: '{Id}', Name: '{Name}', Count: {Count}",
                correlationId,
                grupo.Key.SourceCodeIdNormalized,
                grupo.Key.Id,
                grupo.Key.NameNormalized ?? "NULL",
                grupo.Count());
                foreach(var registro in grupo.Select(g => g.OriginalRecord))
                {
                    duplicados.Add(registro);
                }
            }
        }

        return duplicados;
    }

    #endregion
}