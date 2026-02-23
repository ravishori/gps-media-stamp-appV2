namespace GpsMediaStamp.Web.Services
{
    public interface ILocationService
    {
        Task<LocationResult> ReverseGeocodeAsync(double latitude, double longitude);
    }

    public class LocationResult
    {
        public string Road { get; set; }
        public string Suburb { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Postcode { get; set; }
        public string Country { get; set; }
    }
}