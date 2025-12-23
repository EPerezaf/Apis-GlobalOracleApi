using System.Text.Json.Serialization;

namespace Shared.Contracts.Responses
{
    /// <summary>
    /// Respuesta estándar para todas las APIs (usa "data" en lugar de "results")
    /// </summary>
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; } = true;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("pagination")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PaginationInfo? Pagination { get; set; }

        [JsonPropertyName("errors")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ErrorDetail>? Errors { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta sin tipo genérico para casos simples
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
    }

    /// <summary>
    /// Información de paginación
    /// </summary>
    public class PaginationInfo
    {
        [JsonPropertyName("page")]
        public int Page { get; set; } = 1;

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; } = 50;

        [JsonPropertyName("totalRecords")]
        public int TotalRecords { get; set; } = 0;

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; } = 1;
    }

    /// <summary>
    /// Detalle de un error en la respuesta
    /// </summary>
    public class ErrorDetail
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("field")]
        public string? Field { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("details")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object>? Details { get; set; }
    }
}

