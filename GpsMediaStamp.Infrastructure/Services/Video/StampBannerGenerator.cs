using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using SixLabors.ImageSharp.PixelFormats;

namespace GpsMediaStamp.Infrastructure.Services.Video
{
    public class StampBannerGenerator
    {
        public async Task<string> GenerateBannerAsync(
            string outputPath,
            string fullStampText,
            string qrPath)
        {
            int width = 400;   // slightly smaller → better wrapping
            int height = 180;

            using var image = new Image<Rgba32>(width, height);

            image.Mutate(ctx =>
            {
                // 🔹 Fully transparent background
                ctx.Fill(new Rgba32(28, 32, 38, 200));// 🔹 Thin gold border (drawn inside bounds)
                int borderThickness = 3;
                ctx.Draw(
                    Pens.Solid(new Rgba32(212, 175, 55), borderThickness),
                    new RectangleF(
                        borderThickness / 2f,
                        borderThickness / 2f,
                        width - borderThickness,
                        height - borderThickness
                    )
                );

                // 🔹 Load QR
                using var qr = Image.Load(qrPath);
                qr.Mutate(x => x.Resize(90, 90)); // smaller QR
                ctx.DrawImage(qr, new Point(20, 45), 1f);

                // 🔹 Smaller font
                var font = SystemFonts.CreateFont("Arial", 18);

                var textOptions = new RichTextOptions(font)
                {
                    Origin = new PointF(130, 30),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    WrappingLength = width - 150,
                    LineSpacing = 1.2f
                };
                fullStampText = fullStampText.Replace(",", ",\n");
                ctx.DrawText(textOptions, fullStampText, Color.White);
            });

            await image.SaveAsPngAsync(outputPath);
            return outputPath;
        }
    }
}