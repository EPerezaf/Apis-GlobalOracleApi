namespace Shared.Exceptions
{
    /// <summary>
    /// Excepción cuando no se encuentra un recurso
    /// </summary>
    public class NotFoundException : BusinessException
    {
        public string ResourceName { get; set; }
        public string? ResourceId { get; set; }

        public NotFoundException(string message, string resourceName, string? resourceId = null)
            : base(message, "NOT_FOUND")
        {
            ResourceName = resourceName;
            ResourceId = resourceId;
        }
    }
}

