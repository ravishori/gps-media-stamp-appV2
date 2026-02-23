using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GpsMediaStamp.Web.Services
{
    public class ImageStampService : IImageStampService
    {
        public async Task<string> StampImageAsync(
            string inputPath,
            string stampText,
            string qrPath,
            string qrContent = null)
        {
            var outputPath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}.jpg");

            using var image = await Image.LoadAsync<Rgba32>(inputPath);

            int width = image.Width;
            int height = image.Height;

            int padding = 50;
            int panelHeight = (int)(height * 0.22);

            // =============================================================
            // 1. Subtle watermark
            // =============================================================
            try
            {
                var watermarkFont = SystemFonts.CreateFont("Arial", height / 14f, FontStyle.Bold);

                image.Mutate(ctx =>
                {
                    for (int y = -height; y < height * 2; y += 380)
                    {
                        ctx.DrawText(
                            "GPS Media Stamp - RAVI SHORI",
                            watermarkFont,
                            Color.White.WithAlpha(0.04f),
                            new PointF(0, y));
                    }
                });
            }
            catch { }

            // =============================================================
            // 2. Bottom Panel
            // =============================================================
            var panelRect = new Rectangle(0, height - panelHeight, width, panelHeight);

            image.Mutate(ctx =>
            {
                ctx.Fill(Color.Black.WithAlpha(0.92f), panelRect);
                ctx.Draw(Color.Yellow, 4, panelRect);
            });

            // =============================================================
            // 3. QR Code
            // =============================================================
            Image<Rgba32> qrImage;

            if (!string.IsNullOrWhiteSpace(qrContent))
            {
                throw new NotImplementedException("Dynamic QR not enabled.");
            }
            else
            {
                qrImage = await Image.LoadAsync<Rgba32>(qrPath);
            }

            int qrSize = panelHeight - 2 * padding;

            using (qrImage)
            {
                qrImage.Mutate(x => x.Resize(qrSize, qrSize));

                int qrX = padding;
                int qrY = panelRect.Top + padding;

                image.Mutate(ctx =>
                    ctx.DrawImage(qrImage, new Point(qrX, qrY), 1f));
            }

            // =============================================================
            // 4. India Flag
            // =============================================================
            try
            {
                string flagPath = @"D:\ravishori\GpsMediaStamp\GpsMediaStamp.Web\Resources\Assets\india-flag.png";

                using var flagImage = await Image.LoadAsync<Rgba32>(flagPath);

                int flagWidth = 80;
                int flagHeight = (int)(flagWidth * 0.6);

                flagImage.Mutate(x => x.Resize(flagWidth, flagHeight));

                image.Mutate(ctx =>
                {
                    ctx.DrawImage(
                        flagImage,
                        new Point(padding, panelRect.Top - flagHeight - 12),
                        1f);
                });
            }
            catch { }

            // =============================================================
            // 5. Text
            // =============================================================
            float fontSize = Math.Clamp(height / 55f, 16f, 26f);
            var font = SystemFonts.CreateFont("Arial", fontSize, FontStyle.Regular);

            float textStartX = padding + qrSize + 30;
            float textStartY = panelRect.Top + padding + 8;
            float maxTextWidth = width - textStartX - padding;

            var lines = stampText.Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.RemoveEmptyEntries);

            image.Mutate(ctx =>
            {
                float currentY = textStartY;

                foreach (var line in lines)
                {
                    var wrapped = WrapText(line, font, maxTextWidth);

                    foreach (var subLine in wrapped)
                    {
                        ctx.DrawText(subLine, font, Color.White, new PointF(textStartX, currentY));
                        currentY += fontSize + 10;
                    }

                    currentY += 4;
                }
            });

            await image.SaveAsJpegAsync(outputPath, new JpegEncoder
            {
                Quality = 92
            });

            return outputPath;
        }

        private string[] WrapText(string text, Font font, float maxWidth)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<string>();

            var words = text.Split(' ');
            var result = new System.Collections.Generic.List<string>();
            string currentLine = "";

            foreach (var word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine)
                    ? word
                    : $"{currentLine} {word}";

                var size = TextMeasurer.MeasureSize(testLine, new TextOptions(font));

                if (size.Width > maxWidth && !string.IsNullOrEmpty(currentLine))
                {
                    result.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine = testLine;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
                result.Add(currentLine);

            return result.ToArray();
        }
    }
}