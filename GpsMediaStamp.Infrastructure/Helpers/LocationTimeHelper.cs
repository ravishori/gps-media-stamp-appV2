using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoTimeZone;
using System;
namespace GpsMediaStamp.Infrastructure.Helpers
{
    public static class LocationTimeHelper
    {
        public static DateTime GetLocalTime(double latitude, double longitude)
        {
            try
            {
                var tz = TimeZoneLookup.GetTimeZone(latitude, longitude);
                var timezone = TimeZoneInfo.FindSystemTimeZoneById(tz.Result);

                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
            }
            catch
            {
                return DateTime.UtcNow;
            }
        }
    }
}
