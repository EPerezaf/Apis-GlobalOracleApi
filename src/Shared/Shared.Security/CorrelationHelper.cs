using Microsoft.AspNetCore.Http;
using System.Threading;

namespace Shared.Security
{
    /// <summary>
    /// Helper para generar identificadores únicos de correlación para rastrear el flujo de eventos
    /// </summary>
    public static class CorrelationHelper
    {
        private static long _counter = 0;
        private const string CorrelationIdHeader = "X-Correlation-Id";

        /// <summary>
        /// Obtiene el CorrelationId del HttpContext o genera uno nuevo si no existe
        /// </summary>
        /// <param name="httpContext">Contexto HTTP de la petición</param>
        /// <returns>CorrelationId existente o generado</returns>
        public static string GetCorrelationId(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                return GenerateCorrelationId();
            }

            // Intentar obtener del header
            if (httpContext.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue) && 
                !string.IsNullOrWhiteSpace(headerValue))
            {
                return headerValue.ToString();
            }

            // Intentar obtener de los Items del contexto (si fue establecido previamente)
            if (httpContext.Items.TryGetValue(CorrelationIdHeader, out var itemValue) && 
                itemValue is string correlationId && !string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId;
            }

            // Generar uno nuevo y guardarlo en el contexto
            var newCorrelationId = GenerateCorrelationId();
            httpContext.Items[CorrelationIdHeader] = newCorrelationId;
            httpContext.Response.Headers[CorrelationIdHeader] = newCorrelationId;
            
            return newCorrelationId;
        }

        /// <summary>
        /// Genera un identificador único de correlación con timestamp en hora de México
        /// Formato: CORR-YYYYMMDD-HHMMSS-XXXXXX
        /// </summary>
        public static string GenerateCorrelationId()
        {
            var timestamp = DateTimeHelper.GetMexicoDateTime().ToString("yyyyMMdd-HHmmss");
            var uniqueId = Interlocked.Increment(ref _counter) % 1000000;
            return $"CORR-{timestamp}-{uniqueId:D6}";
        }

        /// <summary>
        /// Genera un identificador único para endpoints con timestamp en hora de México
        /// </summary>
        public static string GenerateEndpointId(string endpointName)
        {
            var timestamp = DateTimeHelper.GetMexicoDateTime().ToString("yyyyMMdd-HHmmss");
            var uniqueId = Interlocked.Increment(ref _counter) % 1000000;
            return $"EP-{endpointName}-{timestamp}-{uniqueId:D6}";
        }
    }
}

