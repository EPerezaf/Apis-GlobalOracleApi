using System.Diagnostics;
using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace GM.CatalogSync.Application.Services;

public class AsignacionService: IAsignacionService
{
    private readonly IAsignacionRepository _repository;
    private readonly ILogger<AsignacionService> _logger;

    public AsignacionService( 
        IAsignacionRepository repository,
        ILogger<AsignacionService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<(List<AsignacionRespuestaDto> data, int totalRecords)> ObtenerAsignacionesAsync(
        string? usuario,
        //string? dealer,
        int page,
        int pageSize,
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[{CorrelationId}] [SERVICE] Iniciando ObtenerAsignacionesAsync - Usuario: {Usuario}, Pagina: {Page}/{PageSize}",
            correlationId, usuario ?? "Todos", page, pageSize);
            //VALIDAR PARAMETROS DE PAGINACION 
            if(page < 1)
            {
                throw new AsignacionValidacionException("El numero de pagina debe ser mayor o igual a 1",
                    new List<ValidationError> { new ValidationError { Field = "page", Message = "debe ser >= 1"}});
                    
            }

            if(pageSize < 1 || pageSize > 50000)
                throw new AsignacionValidacionException("El tamaño de pagina debe ser estar entre 1 y 500000",
                    new List<ValidationError> { new ValidationError { Field = "page", Message = "debe ser >= 1"}});

            //CONSULTAR DESDE REPOSITORY
            var (asignacion, totalRecords) = await _repository.GetByFilterAsync(
                usuario, page, pageSize, correlationId);

            var responseDtos = asignacion.Select(p => new AsignacionRespuestaDto
            {
                Usuario = p.Usuario,
                Dealer = p.Dealer,
                FechaAlta = p.FechaAlta,
                UsuarioAlta = p.UsuarioAlta,
                FechaModificacion = p.FechaModificacion,
                UsuarioModificacion = p.UsuarioModificacion,
                EmpresaId = p.EmpresaId
            }).ToList();
            
            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] [SERVICE] ObtenerAsignacionesAsynccompletado en {Tiempo}ms - {Count} registros de {Total} totales",
                correlationId, stopwatch.ElapsedMilliseconds, responseDtos.Count, totalRecords);
            return (responseDtos, totalRecords);
        }
        catch (AsignacionValidacionException)
        {
            throw;
        }
        catch (AsignacionDataAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogInformation("[{CorrelatioId}] [SERVICE] Error inesperado en ObtenerAsignacionesAsync", correlationId);
            throw new BusinessException("Error al obtener asignaciones", ex);
        }
    }

    public async Task<(List<AsignacionRespuestaDto> data, int totalRecords)> ObtenerUsuariosDisponiblesAsync(
        string? userId,
        string? nombre,
        string? email,
        int? empresaId,
        int page,
        int pageSize,
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[{CorrelationId}] [SERVICE] Iniciando ObtenerUsuariosDisponiblesAsync - Usuario: {User}, Nombre: {Nombre}, Email: {Email}, Pagina: {Page}/{PageSize}",
            correlationId, userId ?? "Todos", nombre ?? "Todos", email ?? "Todos", page, pageSize);

            var (asignacion, totalRecords) = await _repository.GetUsuarioDisponibleByFilterAsync(
                userId,
                nombre,
                email,
                empresaId,
                page,
                pageSize,
                currentUser,
                correlationId);

            var responseDto = asignacion.Select(p => new AsignacionRespuestaDto
            {
                Usuario = p.Usuario,
                Dealer = p.Dealer,
                FechaAlta = p.FechaAlta,
                UsuarioAlta = p.UsuarioAlta,
                FechaModificacion = p.FechaModificacion,
                UsuarioModificacion = p.UsuarioModificacion,
                EmpresaId = p.EmpresaId
            }).ToList();

            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] [SERVICE] ObtenerUsuariosDisponiblesAsync completado en {Tiempo}ms",
                correlationId, stopwatch.ElapsedMilliseconds);
            return (responseDto, totalRecords);
        }
        catch (AsignacionValidacionException)
        {
            throw;
        }
        catch (AsignacionDataAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] [SERVICE] Error inesperado en ObtenerUsuarioDisponibleAsync", correlationId);
            throw new BusinessException("Error al obtener usuario disponible para asignacion", ex);
        }    
    }

    public async Task<(List<DetalleDealerRespuestaDto> data, int totalRecords)> ObtenerDistribuidoresAsignablesAsync(
        string? userId,
        int? empresaId,
        int page,
        int pageSize,
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[{CorrelationId}] [SERVICE] Iniciando ObtenerDistribuidoresAsignablesAsync - UserId: {UserId}, Pagina: {Page}/{PageSize}",
            correlationId, userId ?? "Todos", page, pageSize);

            var (asignacion, totalRecords) = await _repository.GetDealerDisponibleByFilterAsync(
                userId,
                empresaId,
                page,
                pageSize,
                currentUser,
                correlationId);

            var responseDto = asignacion.Select(p => new DetalleDealerRespuestaDto
            {
                DealerId = p.DealerId,
                Nombre = p.Nombre,
                RazonSocial = p.RazonSocial,
                Rfc = p.Rfc,
                EmpresaId = p.EmpresaId
            }).ToList();

            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] [SERVICE] ObtenerDistribuidoresAsignablesAsync completado en {Tiempo}ms",
                correlationId, stopwatch.ElapsedMilliseconds);
            return (responseDto, totalRecords);
        }
        catch (AsignacionValidacionException)
        {
            throw;
        }
        catch (AsignacionDataAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] [SERVICE] Error inesperado en ObtenerDistribuidoresAsignablesAsync", correlationId);
            throw new BusinessException("Error al obtener dealers disponibles para asignacion", ex);
        }    
    }
    

    public async Task<AsignacionBatchResultadoDto> ProcesarBatchInsertAsync(
        List<AsignacionCrearDto> asignacion,
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[{CorrelationId}] [SERVICE] Iniciando ProcesarBatchInsertAsync - Usuario: {User}, Total: {Count}",
            correlationId, currentUser, asignacion?.Count ?? 0);

            //VALIDAR BATCH
            if(asignacion == null || asignacion.Count == 0)
            {
                throw new AsignacionValidacionException("La lista de asignaciones no puede estar vacia",
                    new List<ValidationError> { new ValidationError { Field = "json", Message = "debe contener al menos una asignacion"}});
                
            }

            if(asignacion.Count > 10000)
                throw new AsignacionValidacionException("El tamaño del lote no puede exceder los 10000 registro(s)",
                    new List<ValidationError> { new ValidationError { Field = "json", Message = "maximo 10000 registros"}});

            //VALIDAR CAMPOS OBLIGATORIOS
            var erroresValidacion = ValidarRegistrosBatch(asignacion, correlationId);
            if (erroresValidacion.Any())
            {
                throw new AsignacionValidacionException(
                    $"Se encontraron {erroresValidacion.Count} errores de validacion",
                    erroresValidacion);
            }
            //DETECTAR DUPLICADOS EN EL LOTE 
            var duplicados = DetectarDuplicadosEnBatch(asignacion, correlationId);
            if (duplicados.Any())
            {
                throw new AsignacionDuplicadoException($"Se encontraron {duplicados.Count} asignaciones duplicados en el lote", duplicados.Count, duplicados);
            }
            //VALIDAR DUPLICADOS CONTRA LA BASE DE DATOS 
            /*var duplicadosEnBD = await VaidarDuplicadosEnBaseDatosAsync(asignacion, correlationId);
            if (duplicadosEnBD.Any())
            {
                var mensaje = $"Se encontraron {duplicadosEnBD.Count} asignacion(es) duplicado(s) que ya existen en la base de datos." +
                $"La combinacion de "
            }*/

            //MAPEAR DTOS A ENTITIES
            var asignacionEntities = asignacion.Select(p => new Asignacion
            {
                Usuario = p.Usuario.Trim(),
                Dealer = p.Dealer.Trim(),
            }).ToList();

            //EJECUTAR INSERT BATCH
            var registrosInsertado = await _repository.UpsertBatchWithTransactionAsync(
                asignacionEntities, currentUser, correlationId);

            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] [SERVICE] ProcesarBatchInsertAsync completado en {Tiempo}ms - Insertados: {Ins}",
            correlationId, stopwatch.ElapsedMilliseconds, registrosInsertado);

            return new AsignacionBatchResultadoDto
            {
                RegistrosTotales = asignacion.Count,
                RegistrosInsertados = registrosInsertado,
                RegistrosActualizados = 0,
                RegistrosError = asignacion.Count - registrosInsertado,
                OmitidosPorError = 0
            };
            
        }
        catch (AsignacionDuplicadoException)
        {
            throw;
        }
        catch (AsignacionConflictException)
        {
            throw;
        }
        catch (AsignacionValidacionException)
        {
            throw;
        }
        catch (AsignacionDataAccessException)
        {
            throw;
        }
        catch(Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{CorrelationId}] [SERVICE] Error inesperado en el ProcesarBatchInsertAsync", correlationId);
            throw new BusinessException("Error al procesar lote de asigaciones", ex);
        }
    }
    
    public async Task<int> EliminarTodosAsync(
        string usuario,
        string dealer,
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[{CorrelationId}] [SERVICE] Iniciando EliminarTodosAsync - Usuario: {User}",
                correlationId, currentUser);
            var rowsAffected = await _repository.DeleteAllAsync(usuario, dealer,currentUser, correlationId);

            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] [SERVICE] EliminarTodosAsync completado en {Tiempo}ms - {Rows} filas eliminadas",
                correlationId, stopwatch.ElapsedMilliseconds, rowsAffected);
            return rowsAffected;
        }
        catch (AsignacionDataAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{CorrelationId}] [SERVICE] Error inesperado en EliminarTodosAsync", correlationId);
            throw new BusinessException("Error al eliminar asignaciones", ex);
        }
    }
    

    #region Private Helpers

    private List<ValidationError> ValidarRegistrosBatch(List<AsignacionCrearDto> asignaciones, string correlationId)
    {
        var errors = new List<ValidationError>();

        for(int i = 0; i < asignaciones.Count; i++)
        {
            var asignacion = asignaciones[i];
            var index = i + 1;

            if(string.IsNullOrWhiteSpace(asignacion.Usuario))
                errors.Add(new ValidationError { Field = $"json[{index}].usuario", Message = "es requerido"});
            if(string.IsNullOrWhiteSpace(asignacion.Dealer))
                errors.Add(new ValidationError { Field = $"json[{index}].dealer", Message = "es requerido"});
        }

        return errors;
    }

    private List<AsignacionCrearDto> DetectarDuplicadosEnBatch(List<AsignacionCrearDto> asignacion, string correlationId)
    {
        var normalizedRecords = asignacion
            .Select((r, idx) => new
            {
                Index = idx,
                OriginalRecord = r,
                UsuarioNormalized = (r.Usuario ?? string.Empty).Trim(),
                DealerNormalized = (r.Dealer ?? string.Empty).Trim()
            }).ToList();
        
        var duplicadosGrupos = normalizedRecords
            .GroupBy(r => new { r.UsuarioNormalized, r.DealerNormalized})
            .Where( g => g.Count() > 1)
            .ToList();
        var duplicados = new List<AsignacionCrearDto>();
        if (duplicadosGrupos.Any())
        {
            foreach (var grupo in duplicadosGrupos)
            {
                _logger.LogWarning("[{CorrelationId}] [SERVICE] Duplicados en batch - Usuario: '{Usuario}', Dealer: {Dealer} - Cantidad: {Count}",
                correlationId,
                grupo.Key.UsuarioNormalized,
                grupo.Key.DealerNormalized,
                grupo.Count());

                foreach (var registro in grupo.Select( g => g.OriginalRecord))
                {
                    duplicados.Add(registro);
                }
            }
        }
        return duplicados;
    }

    #endregion
}