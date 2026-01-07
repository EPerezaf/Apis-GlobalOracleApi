using GM.CatalogSync.Application.DTOs;
using GM.CatalogSync.Application.Exceptions;
using GM.CatalogSync.Domain.Entities;
using GM.CatalogSync.Domain.Interfaces;
using Shared.Exceptions;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace GM.CatalogSync.Application.Services;

/// <summary>
/// Service para lógica de negocio de Campaign
/// </summary>
public class CampaignService : ICampaignService
{
    private readonly ICampaignRepository _repository;
    private readonly ILogger<CampaignService> _logger;

    public CampaignService(
        ICampaignRepository repository,
        ILogger<CampaignService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<(List<CampaignResponseDto> data, int totalRecords)> GetCampaignsAsync(
        string? id,
        string? leadRecordType,
        int page,
        int pageSize,
        string currentUser,
        string correlationId)

    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
           _logger.LogInformation("[{CorrelationId}] 🔷 [SERVICE] Iniciando GetCampaignsAsync - Id: {Id}, LeadRecordType: {LeadRecordType}, Página: {Page}/{PageSize}",
                correlationId, id ?? "Todos", leadRecordType ?? "Todos", page, pageSize);

            // Validar parámetros de paginación
            if (page < 1)
                throw new CampaignValidationException("El número de página debe ser mayor o igual a 1",
                    new List<ValidationError> { new ValidationError { Field = "page", Message = "debe ser >= 1" } });

            if (pageSize < 1 || pageSize > 50000)
                throw new CampaignValidationException("El tamaño de página debe estar entre 1 y 50000",
                    new List<ValidationError> { new ValidationError { Field = "pageSize", Message = "debe estar entre 1 y 50000" } });

            // Consultar desde Repository
            var (campaigns, totalRecords) = await _repository.GetByFiltersAsync(
                id, leadRecordType, page, pageSize, correlationId);

            // Mapear a DTOs
            var responseDtos = campaigns.Select(p => new CampaignResponseDto
            {
                CampaniaId = p.CampaniaId,
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
            _logger.LogInformation("[{CorrelationId}] ✅ [SERVICE] GetCampaignsAsync completado en {Tiempo}ms - {Count} registros de {Total} totales",
                correlationId, stopwatch.ElapsedMilliseconds, responseDtos.Count, totalRecords);

            return (responseDtos, totalRecords);
        }
        catch (CampaignValidationException)
        {
            throw;
        }
        catch (CampaignDataAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{CorrelationId}] ❌ [SERVICE] Error inesperado en GetCampaignsAsync", correlationId);
            throw new BusinessException("Error al obtener campanias", ex);
        }
    }

    public async Task<CampaignBatchResultDto> ProcessBatchInsertAsync(
        List<CreateCampaignDto> campaigns,
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[{CorrelationId}] 🔷 [SERVICE] Iniciando ProcessBatchInsertAsync - Usuario: {User}, Total: {Count}",
                correlationId, currentUser, campaigns?.Count ?? 0);

            // Validar batch
            if (campaigns == null || campaigns.Count == 0)
                throw new CampaignValidationException("La lista de campanias no puede estar vacía",
                    new List<ValidationError> { new ValidationError { Field = "json", Message = "debe contener al menos una campaña" } });

            if (campaigns.Count > 10000)
                throw new CampaignValidationException("El tamaño del lote no puede exceder 10000 registros",
                    new List<ValidationError> { new ValidationError { Field = "json", Message = "máximo 10000 registros" } });

            // Validar campos obligatorios
            var erroresValidacion = ValidateBatchRecords(campaigns, correlationId);
            if (erroresValidacion.Any())
            {
                throw new CampaignValidationException(
                    $"Se encontraron {erroresValidacion.Count} errores de validación",
                    erroresValidacion);
            }

            // Detectar duplicados en el lote
            var duplicados = DetectDuplicatesInBatch(campaigns, correlationId);
            if (duplicados.Any())
            {
                throw new CampaignDuplicateException($"Se encontraron {duplicados.Count} campanias duplicadas en el lote", duplicados.Count, duplicados);
            }

            // Validar duplicados contra la base de datos
            var duplicadosEnBD = await ValidateDuplicatesInDatabaseAsync(campaigns, correlationId);
            if (duplicadosEnBD.Any())
            {
                var mensaje = $"Se encontraron {duplicadosEnBD.Count} registro(s) duplicado(s) que ya existen en la base de datos. " +
                              $"El id debe ser único.";
                throw new CampaignDuplicateException(mensaje, duplicadosEnBD.Count);
            }

            // Mapear DTOs a Entities
            var campaignEntities = campaigns.Select(p => new Campaign
            {
                 SourceCodeId = p.SourceCodeId.Trim(),
                Id = p.Id.Trim(),
                Name = p.Name.Trim(),
                RecordTypeId = p.RecordTypeId.Trim(),
                LeadRecordType = p.LeadRecordType.Trim(),
                LeadEnquiryType = string.IsNullOrWhiteSpace(p.LeadEnquiryType) ? null : p.LeadEnquiryType.Trim(),
                LeadSource = string.IsNullOrWhiteSpace(p.LeadSource) ? null : p.LeadSource.Trim(),
                LeadSourceDetails = string.IsNullOrWhiteSpace(p.LeadSourceDetails) ? null : p.LeadSourceDetails.Trim(),
                Status = string.IsNullOrWhiteSpace(p.Status) ? null : p.Status.Trim()
            }).ToList();

            // Ejecutar INSERT batch
            var registrosInsertados = await _repository.UpsertBatchWithTransactionAsync(
                campaignEntities, currentUser, correlationId);

            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] ✅ [SERVICE] ProcesarBatchInsertAsync completado en {Tiempo}ms - Insertados: {Ins}",
                correlationId, stopwatch.ElapsedMilliseconds, registrosInsertados);

            return new CampaignBatchResultDto
            {
                RegistrosTotales = campaigns.Count,
                RegistrosInsertados = registrosInsertados,
                RegistrosActualizados = 0,
                RegistrosError = campaigns.Count - registrosInsertados,
                OmitidosPorError = 0
            };
        }
        catch (CampaignDuplicateException)
        {
            throw;
        }
        catch (CampaignValidationException)
        {
            throw;
        }
        catch (CampaignDataAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{CorrelationId}] ❌ [SERVICE] Error inesperado en ProcessBatchInsertAsync", correlationId);
            throw new BusinessException("Error al procesar lote de campanias", ex);
        }
    }

    public async Task<int> DeleteAllAsync(
        string currentUser,
        string correlationId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("[{CorrelationId}] 🔷 [SERVICE] Iniciando DeleteAllAsync - Usuario: {User}",
                correlationId, currentUser);

            var rowsAffected = await _repository.DeleteAllAsync(currentUser, correlationId);

            stopwatch.Stop();
            _logger.LogInformation("[{CorrelationId}] ✅ [SERVICE] DeleteAllAsync completado en {Tiempo}ms - {Rows} filas eliminadas",
                correlationId, stopwatch.ElapsedMilliseconds, rowsAffected);

            return rowsAffected;
        }
        catch (CampaignDataAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{CorrelationId}] ❌ [SERVICE] Error inesperado en DeleteAllAsync", correlationId);
            throw new BusinessException("Error al eliminar campanias", ex);
        }
    }

    #region Private Helpers

    private List<ValidationError> ValidateBatchRecords(List<CreateCampaignDto> campaigns, string correlationId)
    {
        var errors = new List<ValidationError>();

        for (int i = 0; i < campaigns.Count; i++)
        {
            var campaign = campaigns[i];
            var index = i + 1;

if (string.IsNullOrWhiteSpace(campaign.SourceCodeId))
                errors.Add(new ValidationError { Field = $"json[{index}].sourceCodeId", Message = "es requerido" });

            if (string.IsNullOrWhiteSpace(campaign.Id))
                errors.Add(new ValidationError { Field = $"json[{index}].id", Message = "es requerido" });

            if (string.IsNullOrWhiteSpace(campaign.Name))
                errors.Add(new ValidationError { Field = $"json[{index}].name", Message = "es requerido" });

            if (string.IsNullOrWhiteSpace(campaign.RecordTypeId))
                errors.Add(new ValidationError { Field = $"json[{index}].recordTypeId", Message = "es requerido" });

            if (string.IsNullOrWhiteSpace(campaign.LeadRecordType))
                errors.Add(new ValidationError { Field = $"json[{index}].leadRecordType", Message = "es requerido" });

        }

        return errors;
    }

    private List<CreateCampaignDto> DetectDuplicatesInBatch(List<CreateCampaignDto> campaigns, string correlationId)
    {
        var normalizedRecords = campaigns
            .Select((r, idx) => new
            {
                Index = idx,
                OriginalRecord = r,
                IdNormalized = (r.Id ?? string.Empty).Trim()

            })
            .ToList();

       var duplicatedGroups = normalizedRecords
            .GroupBy(r => new { r.IdNormalized })
            .Where(g => g.Count() > 1)
            .ToList();

        var duplicados = new List<CreateCampaignDto>();

        if (duplicatedGroups.Any())
        {
            foreach (var grupo in duplicatedGroups)
            {
                _logger.LogWarning("[{CorrelationId}] ⚠️ [SERVICE] Duplicados encontrados en batch -, Id: '{Id}' - Cantidad: {Count}",
                    correlationId,
                    grupo.Key.IdNormalized ?? "NULL",
                    grupo.Count());


                foreach (var registro in grupo.Select(g => g.OriginalRecord))
                {
                    duplicados.Add(registro);
                }
            }
        }

        return duplicados;
    }

    private async Task<List<CreateCampaignDto>> ValidateDuplicatesInDatabaseAsync(
        List<CreateCampaignDto> campaigns,
        string correlationId)
    {
        var duplicados = new List<CreateCampaignDto>();
        foreach (var campaign in campaigns)
        {
            var idNormalized = (campaign.Id ?? string.Empty).Trim();

            var existe = await _repository.ExistsByIdAsync(
                idNormalized,
                correlationId);



            if (existe)
            {
                _logger.LogWarning("[{CorrelationId}] ⚠️ [SERVICE] Duplicado encontrado en BD - Id: '{Id}'",
                    correlationId, idNormalized  ?? "NULL");
                duplicados.Add(campaign);
            }
        }

        return duplicados;
    }

    #endregion
}

