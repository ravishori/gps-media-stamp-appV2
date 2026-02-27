using GpsMediaStamp.Application.Interfaces.Image;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.PixelFormats;

namespace GpsMediaStamp.Infrastructure.Services.Media
{
    public class ImageOverlayService : IImageStampService
    {
        public async Task<string> StampPremiumImageAsync(
            string inputPath,
            string address,
            double latitude,
            double longitude,
            DateTime timestamp,
            string qrPath)
        {
            var outputPath = Path.Combine(
                Path.GetDirectoryName(inputPath)!,
                "stamped_" + Path.GetFileName(inputPath));

            using var image = await Image.LoadAsync<Rgba32>(inputPath);

            int panelHeight = 220;
            int margin = 40;
            int cornerRadius = 25;

            // 1️⃣ Create floating panel rectangle
            var panelRect = new Rectangle(
                margin,
                image.Height - panelHeight - margin,
                image.Width - (margin * 2),
                panelHeight);

            var panelColor = Color.ParseHex("#111111").WithAlpha(0.85f);

            image.Mutate(ctx =>
            {
                ctx.Fill(panelColor, panelRect);
            });

            // 2️⃣ Fonts
            var titleFont = SystemFonts.CreateFont("Arial", 32, FontStyle.Bold);
            var normalFont = SystemFonts.CreateFont("Arial", 26);

            string addressBlock =
                $"{address}\n" +
                $"Lat {latitude:F6} | Lon {longitude:F6}\n" +
                $"{timestamp:dd MMM yyyy   hh:mm tt} IST";

            // 3️⃣ Draw QR (Left)
            using var qrImage = await Image.LoadAsync<Rgba32>(qrPath);

            int qrSize = 140;
            qrImage.Mutate(x => x.Resize(qrSize, qrSize));

            int qrX = panelRect.X + 30;
            int qrY = panelRect.Y + (panelHeight - qrSize) / 2;

            image.Mutate(ctx =>
            {
                ctx.DrawImage(qrImage, new Point(qrX, qrY), 1f);
            });

            // 4️⃣ Draw Text (Center)
            float textX = qrX + qrSize + 40;
            float textY = panelRect.Y + 40;

            image.Mutate(ctx =>
            {
                ctx.DrawText(
                    addressBlock,
                    normalFont,
                    Color.White,
                    new PointF(textX, textY));
            });

            // 5️⃣ Draw India Logo (Right Watermark)
            string logoPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets",
                "india-logo.png");

            using var logoImage = await Image.LoadAsync<Rgba32>(logoPath);

            int logoSize = 120;
            logoImage.Mutate(x => x.Resize(logoSize, logoSize));

            int logoX = panelRect.Right - logoSize - 30;
            int logoY = panelRect.Y + (panelHeight - logoSize) / 2;

            image.Mutate(ctx =>
            {
                ctx.DrawImage(
                    logoImage,
                    new Point(logoX, logoY),
                    0.9f); // slight transparency
            });

            await image.SaveAsync(outputPath);
            return outputPath;
        }
    }
}