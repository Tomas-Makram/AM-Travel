using System;

namespace BusinessLayer.Services
{
    public class CairoTimeService
    {
        private readonly TimeZoneInfo _cairoTimeZone;

        public CairoTimeService()
        {
            _cairoTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
        }

        public DateTime Now()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _cairoTimeZone);
        }

        public DateTime UtcToCairo(DateTime utcDateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _cairoTimeZone);
        }

        public DateTime CairoToUtc(DateTime cairoDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(cairoDateTime, _cairoTimeZone);
        }
    }
}