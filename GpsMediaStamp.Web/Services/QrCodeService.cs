using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace GpsMediaStamp.Web.Services
{
    public interface IGoogleMapsQrService
    {
        Task<string> GenerateGoogleMapsQrAsync(double lat, double lon);
    }

    public class GoogleMapsQrService : IGoogleMapsQrService
    {
        public async Task<string> GenerateGoogleMapsQrAsync(double lat, double lon)
        {
            var mapsUrl = $"https://www.google.com/maps?q={lat},{lon}";

            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(mapsUrl, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new QRCode(qrData);
            using var qrBitmap = qrCode.GetGraphic(20);

            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
            qrBitmap.Save(tempPath, ImageFormat.Png);

            return tempPath;
        }
    }
}