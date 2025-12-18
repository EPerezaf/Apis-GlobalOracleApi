using Serilog.Core;
using Serilog.Events;
using Shared.Security;

namespace Shared.Infrastructure.Logging
{
    /// <summary>
    /// Enricher para agregar timestamp con hora de México a los logs de Serilog
    /// Reemplaza el Timestamp por defecto con la hora de México
    /// </summary>
    public class MexicoTimeEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var mexicoTime = DateTimeHelper.GetMexicoDateTime();
            
            // Agregar MexicoTime con la hora de México (usaremos esta propiedad en el outputTemplate)
            var mexicoTimeProperty = propertyFactory.CreateProperty("MexicoTime", mexicoTime);
            logEvent.AddPropertyIfAbsent(mexicoTimeProperty);
        }
    }
}

