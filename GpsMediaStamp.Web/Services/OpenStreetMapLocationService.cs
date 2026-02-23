using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GpsMediaStamp.Web.Services
{
    public class OpenStreetMapLocationService : ILocationService
    {
        private readonly HttpClient _http;

        public OpenStreetMapLocationService(HttpClient http)
        {
            _http = http;
        }

        public async Task<LocationResult> ReverseGeocodeAsync(double lat, double lon)
        {
            // Validate coordinate range
            if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
            {
                return new LocationResult();
            }

            _http.DefaultRequestHeaders.UserAgent.ParseAdd("GpsMediaStampApp/1.0");

            var url =
                $"https://nominatim.openstreetmap.org/reverse?format=json&lat={lat}&lon={lon}&addressdetails=1";

            var response = await _http.GetStringAsync(url);

            using var document = JsonDocument.Parse(response);

            if (!document.RootElement.TryGetProperty("address", out var address))
            {
                return new LocationResult();
            }

            return new LocationResult
            {
                Road = address.TryGetProperty("road", out var road)
                    ? road.GetString() ?? string.Empty
                    : string.Empty,

                Suburb = address.TryGetProperty("suburb", out var suburb)
                    ? suburb.GetString() ?? string.Empty
                    : string.Empty,

                City = address.TryGetProperty("city", out var city)
                    ? city.GetString() ?? string.Empty
                    : address.TryGetProperty("town", out var town)
                        ? town.GetString() ?? string.Empty
                        : string.Empty,

                State = address.TryGetProperty("state", out var state)
                    ? state.GetString() ?? string.Empty
                    : string.Empty,

                Postcode = address.TryGetProperty("postcode", out var postcode)
                    ? postcode.GetString() ?? string.Empty
                    : string.Empty,

                Country = address.TryGetProperty("country", out var country)
                    ? country.GetString() ?? string.Empty
                    : string.Empty
            };
        }
    }
}