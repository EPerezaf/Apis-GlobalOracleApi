using Shared.Exceptions;
using GM.CatalogSync.Application.DTOs;

namespace GM.CatalogSync.Application.Exceptions
{
    public class CampaignNotFoundException : NotFoundException
    {
        public CampaignNotFoundException(int campaniaId)
            : base($"No se encontró la campania con ID: {campaniaId}", "Campania", campaniaId.ToString())
        {
        }
    }

    public class CampaignDuplicateException : BusinessException
    {
        public int DuplicateCount { get; set; }
        public List<CreateCampaignDto> DuplicateRecords { get; set; }

        public CampaignDuplicateException(int duplicateCount)
            : base($"Se encontraron {duplicateCount} campanias duplicadas en el lote", "DUPLICATE_CAMPANIA")
        {
            DuplicateCount = duplicateCount;
            DuplicateRecords = new List<CreateCampaignDto>();
        }

        public CampaignDuplicateException(string message, int duplicateCount, List<CreateCampaignDto>? duplicateRecords = null)
            : base(message, "DUPLICATE_CAMPANIA")
        {
            DuplicateCount = duplicateCount;
            DuplicateRecords = duplicateRecords ?? new List<CreateCampaignDto>();
        }
    }

    public class CampaignDataAccessException : DataAccessException
    {
        public CampaignDataAccessException(string message, Exception innerException)
            : base($"Error de acceso a datos en Campania: {message}", innerException)
        {
        }
    }

    public class CampaignValidationException : BusinessValidationException
    {
        public CampaignValidationException(string message, List<ValidationError> errors)
            : base(message, errors)
        {
        }
    }

    public class CampaignBatchException : BusinessException
    {
        public int TotalRecords { get; set; }
        public int ErrorCount { get; set; }
        public int SuccessCount { get; set; }

        public CampaignBatchException(string message, int totalRecords, int errorCount)
            : base(message, "BATCH_ERROR")
        {
            TotalRecords = totalRecords;
            ErrorCount = errorCount;
            SuccessCount = totalRecords - errorCount;
        }
    }
}

