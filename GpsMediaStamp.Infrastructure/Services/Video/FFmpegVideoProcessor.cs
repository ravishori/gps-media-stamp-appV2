using Microsoft.AspNetCore.Hosting;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GpsMediaStamp.Infrastructure.Services.Video
{
    public class FFmpegVideoProcessor
    {

        private async Task WaitForFileRelease(string path)
        {
            int retries = 10;

            while (retries > 0)
            {
                try
                {
                    using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        return;
                    }
                }
                catch
                {
                    await Task.Delay(200);
                    retries--;
                }
            }
        }
        private readonly IWebHostEnvironment _env;

        public FFmpegVideoProcessor(IWebHostEnvironment env)
        {
            _env = env;
        }

        private string Normalize(string path)
        {
            return path.Replace("\\", "/");
        }

        public async Task RunStampAsync(
            string inputPath,
            string outputPath,
            string stampText,
            string fontFileName,
            string qrImagePath)
        {
            var tempFolder = Path.Combine(_env.ContentRootPath, "storage", "temp");
            Directory.CreateDirectory(tempFolder);

            // 🔹 Create overlay PNG
            var overlayPath = Path.Combine(tempFolder, $"overlay_{Guid.NewGuid()}.png");

            await CreateOverlayImage(stampText, fontFileName, overlayPath);

            string safeOverlay = Normalize(overlayPath);

            string arguments =
                $"-y -i \"{inputPath}\" -i \"{safeOverlay}\" " +
                "-filter_complex \"overlay=40:H-h-40\" " +
                "-c:v libx264 -preset medium -crf 22 -pix_fmt yuv420p " +
                "-c:a copy " +
                $"\"{outputPath}\"";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = arguments,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            await WaitForFileRelease(outputPath);
            // Ensure FFmpeg fully releases file
            await Task.Delay(300);

            if (process.ExitCode != 0)
                throw new Exception($"FFmpeg failed:\n{error}");

            if (File.Exists(overlayPath))
                File.Delete(overlayPath);
        }

        private async Task CreateOverlayImage(
    string text,
    string fontFileName,
    string outputPath)
        {
            var fontPath = Path.Combine(
                _env.ContentRootPath,
                "Resources",
                "Fonts",
                fontFileName);

            var collection = new FontCollection();
            var family = collection.Add(fontPath);
            var font = family.CreateFont(30);

            int width = 1000;
            int height = 180;

            using var image = new Image<Rgba32>(width, height);

            image.Mutate(ctx =>
            {
                // Semi-transparent black background
                ctx.Fill(Color.FromRgba(0, 0, 0, 220));

                var options = new SixLabors.ImageSharp.Drawing.Processing.RichTextOptions(font)
                {
                    Origin = new PointF(40, 40),
                    WrappingLength = width - 80
                };

                ctx.DrawText(options, text, Color.White);
            });

            await image.SaveAsPngAsync(outputPath);
        }
    }
}