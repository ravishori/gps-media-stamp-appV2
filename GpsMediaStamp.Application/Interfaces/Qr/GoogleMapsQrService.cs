using System.Threading.Tasks;

namespace GpsMediaStamp.Application.Interfaces.Qr
{
    public interface IGoogleMapsQrService
    {
        Task<string> GenerateGoogleMapsQrAsync(double lat, double lon);
    }

    public class GoogleMapsQrService : IGoogleMapsQrService
    {
        private readonly IQrCodeService _qrService;

        public GoogleMapsQrService(IQrCodeService qrService)
        {
            _qrService = qrService;
        }

        public async Task<string> GenerateGoogleMapsQrAsync(double lat, double lon)
        {
            var mapsUrl = $"https://www.google.com/maps?q={lat},{lon}";
            return await _qrService.GenerateQrAsync(mapsUrl);
        }
    }
}