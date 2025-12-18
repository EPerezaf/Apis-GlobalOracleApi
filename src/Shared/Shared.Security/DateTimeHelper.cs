namespace Shared.Security
{
    /// <summary>
    /// Helper para manejo de fechas y horas en zona horaria de México
    /// </summary>
    public static class DateTimeHelper
    {
        private static readonly TimeZoneInfo MexicoTimeZone;

        static DateTimeHelper()
        {
            try
            {
                if (TimeZoneInfo.TryFindSystemTimeZoneById("Central Standard Time (Mexico)", out var tzWindows))
                {
                    MexicoTimeZone = tzWindows;
                }
                else if (TimeZoneInfo.TryFindSystemTimeZoneById("America/Mexico_City", out var tz))
                {
                    MexicoTimeZone = tz;
                }
                else
                {
                    MexicoTimeZone = TimeZoneInfo.Local;
                }
            }
            catch
            {
                MexicoTimeZone = TimeZoneInfo.Local;
            }
        }

        /// <summary>
        /// Obtiene la fecha y hora actual en la zona horaria de México como DateTime
        /// </summary>
        public static DateTime GetMexicoDateTime()
        {
            try
            {
                var utcNow = DateTime.UtcNow;
                return TimeZoneInfo.ConvertTimeFromUtc(utcNow, MexicoTimeZone);
            }
            catch
            {
                return DateTime.Now;
            }
        }

        /// <summary>
        /// Obtiene la fecha y hora actual en la zona horaria de México formateada como ISO 8601
        /// </summary>
        public static string GetMexicoTimeString()
        {
            try
            {
                var mexicoTime = GetMexicoDateTime();
                return mexicoTime.ToString("yyyy-MM-ddTHH:mm:ss");
            }
            catch
            {
                return DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            }
        }
    }
}

