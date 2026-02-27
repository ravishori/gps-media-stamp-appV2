using GpsMediaStamp.Application.Interfaces.Video;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace GpsMediaStamp.Infrastructure.Services.Video
{
    public class VideoStampService : IVideoStampService
    {
        private readonly IWebHostEnvironment _env;

        public VideoStampService(IWebHostEnvironment env)
        {
            _env = env;
        }
        private string EscapeTextFinal(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("\\", "\\\\")
                .Replace(":", "\\:")
                .Replace("'", "\\'")
                .Replace(",", "\\,")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n")
                .Replace("\r", "")
                .Trim();
        }

        // ✅ Wrap by character width
        private string WrapText(string text, int maxCharsPerLine = 30, int maxLines = 4)
        {
            Console.WriteLine("=== WRAP FUNCTION START ===");
            Console.WriteLine("Original Address:");
            Console.WriteLine(text);

            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var lines = new List<string>();
            var currentLine = new StringBuilder();

            foreach (var word in words)
            {
                if (currentLine.Length + word.Length + 1 > maxCharsPerLine)
                {
                    lines.Add(currentLine.ToString().Trim());
                    currentLine.Clear();

                    if (lines.Count == maxLines)
                        break;
                }

                currentLine.Append(word + " ");
            }

            if (currentLine.Length > 0 && lines.Count < maxLines)
                lines.Add(currentLine.ToString().Trim());

            var result = string.Join("\n", lines);

            Console.WriteLine("Wrapped Address:");
            Console.WriteLine(result);
            Console.WriteLine("=== WRAP FUNCTION END ===");

            return result;
        }

        public async Task<string> StampVideoAsync(
            string inputPath,
            string stampText,
            string qrImagePath)
        {
            Console.WriteLine("StampVideoAsync executing...");

            var stampedFolder = Path.Combine(
                _env.ContentRootPath,
                "storage",
                "stamped");

            var outputPath = Path.Combine(
                stampedFolder,
                $"{Guid.NewGuid()}.mp4");

            // Split incoming stampText
            var lines = stampText.Split('\n');

            // Wrap only the first line (address)
            var address = lines.Length > 0 ? lines[0] : "";
            var wrappedAddress = WrapText(address, 30);

            // Rebuild final text
            var finalTextBuilder = new StringBuilder();
            finalTextBuilder.AppendLine(wrappedAddress);

            for (int i = 1; i < lines.Length; i++)
            {
                finalTextBuilder.AppendLine(lines[i]);
            }

            var finalStampText = finalTextBuilder.ToString().Trim();

            Console.WriteLine("FINAL TEXT:");
            Console.WriteLine(finalStampText);

            // Only escape here (NO second wrapping)
            var safeText = EscapeText(finalStampText);

            Console.WriteLine("SAFE TEXT:");
            Console.WriteLine(safeText);

            var logoPath = Path.Combine(
                _env.ContentRootPath,
                "Assets",
                "india-logo.png");

            if (!File.Exists(logoPath))
                throw new FileNotFoundException($"India logo not found at: {logoPath}");

            // 1️⃣ Create banner PNG path
            var bannerPath = Path.Combine(
                Path.GetTempPath(),
                $"{Guid.NewGuid()}_stamp.png");

            // 2️⃣ Create instance
            var bannerGenerator = new StampBannerGenerator();

            // 3️⃣ Generate banner image
            await bannerGenerator.GenerateBannerAsync(
                bannerPath,
                finalStampText,
                qrImagePath);

            var ffmpegArgs =
            $"-y -i \"{inputPath}\" -i \"{bannerPath}\" " +
            "-filter_complex \"overlay=20:H-h-20\" " +
            "-c:v libx264 -preset veryfast -crf 18 -codec:a copy " +
            $"\"{outputPath}\"";

            Console.WriteLine("FFMPEG ARGS:");
            Console.WriteLine(ffmpegArgs);

            using var process = new Process();
            process.StartInfo.FileName = "ffmpeg";
            process.StartInfo.Arguments = ffmpegArgs;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            string stdError = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception("FFmpeg failed:\n" + stdError);

            return outputPath;
        }

        private string EscapeText(string text)
        {
            return text
                .Replace("\\", "\\\\")
                .Replace(":", "\\:")
                .Replace("'", "\\'")
                .Replace(",", "\\,");
        }
    }
}