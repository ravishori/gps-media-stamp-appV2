using GpsMediaStamp.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace GpsMediaStamp.Infrastructure.Services
{
    public class GoogleGeocodingService : ILocationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GoogleGeocodingService(
            HttpClient httpClient,
            IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["GoogleMaps:ApiKey"]
                      ?? throw new InvalidOperationException(
                          "GoogleMaps:ApiKey is not configured.");
        }

        public async Task<string?> ReverseGeocodeAsync(
            double latitude,
            double longitude)
        {
            var url =
                $"https://maps.googleapis.com/maps/api/geocode/json?" +
                $"latlng={latitude},{longitude}" +
                $"&result_type=street_address|premise|route" +
                $"&location_type=ROOFTOP|RANGE_INTERPOLATED" +
                $"&key={_apiKey}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(content))
                return null;

            var json = JObject.Parse(content);

            if (json["status"]?.ToString() != "OK")
            {
                Console.WriteLine("Geocoding API failed: " + json["status"]);
                return null;
            }

            var results = json["results"] as JArray;

            if (results == null || !results.Any())
                return null;

            var components = results.First["address_components"] as JArray;

            if (components == null)
                return null;

            string? GetComponent(string type)
            {
                var comp = components
                    .FirstOrDefault(c =>
                        c["types"] != null &&
                        c["types"]!.Any(t => t!.ToString() == type));

                return comp?["long_name"]?.ToString();
            }

            // 🏗 Extract components (India-compatible priority)
            var subPremise = GetComponent("subpremise");  // Flat number
            var premise = GetComponent("premise");        // Building name
            var route = GetComponent("route");            // Road
            var subLocality = GetComponent("sublocality")
                              ?? GetComponent("sublocality_level_1");
            var locality = GetComponent("locality");
            var postalCode = GetComponent("postal_code");
            var state = GetComponent("administrative_area_level_1");
            var country = GetComponent("country");

            var lines = new List<string>();

            // 🏢 Line 1 → Flat + Building OR Route
            string line1 = "";

            if (!string.IsNullOrWhiteSpace(subPremise))
                line1 += subPremise + ", ";

            if (!string.IsNullOrWhiteSpace(premise))
                line1 += premise;
            else if (!string.IsNullOrWhiteSpace(route))
                line1 += route;

            line1 = line1.Trim().Trim(',');

            if (!string.IsNullOrWhiteSpace(line1))
                lines.Add(line1);

            // 📍 Line 2 → SubLocality
            if (!string.IsNullOrWhiteSpace(subLocality))
                lines.Add(subLocality);

            // 🏙 Line 3 → Locality + Postal
            string cityLine = "";

            if (!string.IsNullOrWhiteSpace(locality))
                cityLine += locality;

            if (!string.IsNullOrWhiteSpace(postalCode))
                cityLine += " " + postalCode;

            if (!string.IsNullOrWhiteSpace(cityLine))
                lines.Add(cityLine.Trim());

            // 🌎 Line 4 → State + Country
            string stateLine = "";

            if (!string.IsNullOrWhiteSpace(state))
                stateLine += state;

            if (!string.IsNullOrWhiteSpace(country))
                stateLine += ", " + country;

            stateLine = stateLine.Trim().Trim(',');

            if (!string.IsNullOrWhiteSpace(stateLine))
                lines.Add(stateLine);

            var finalAddress = string.Join("\n", lines);

            Console.WriteLine("===== CLEAN PARSED ADDRESS =====");
            Console.WriteLine(finalAddress);
            Console.WriteLine("================================");

            return string.IsNullOrWhiteSpace(finalAddress)
                ? null
                : finalAddress;
        }
    }
}