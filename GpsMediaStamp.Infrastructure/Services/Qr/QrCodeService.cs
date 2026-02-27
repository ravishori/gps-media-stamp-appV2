using GpsMediaStamp.Application.Interfaces.Qr;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace GpsMediaStamp.Infrastructure.Services.Qr
{
    public class QrCodeService : IQrCodeService
    {
        public async Task<string> GenerateQrAsync(string payload)
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new QRCode(qrData);
            using var qrBitmap = qrCode.GetGraphic(20);

            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
            qrBitmap.Save(tempPath, ImageFormat.Png);

            return tempPath;
        }
    }
}