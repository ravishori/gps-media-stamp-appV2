namespace GpsMediaStamp.Application.Interfaces
{
    public interface ILocationService
    {
        Task<string?> ReverseGeocodeAsync(double latitude, double longitude);
    }
}