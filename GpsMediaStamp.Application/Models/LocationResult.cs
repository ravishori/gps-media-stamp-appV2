namespace GpsMediaStamp.Application.Models
{
    public class LocationResult
    {
        public string? Road { get; set; }
        public string? Suburb { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Postcode { get; set; }
        public string? Country { get; set; }
        public string? FormattedAddress { get; set; }

        public string ToDisplayString()
        {
            return
                $"{Road ?? ""}\n" +
                $"{Suburb ?? ""}\n" +
                $"{City ?? ""}, {State ?? ""} {Postcode ?? ""}\n" +
                $"{Country ?? ""}";
        }
    }
}