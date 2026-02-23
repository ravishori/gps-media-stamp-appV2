using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace GpsMediaStamp.Web.Services
{
    public class VideoStampService : IVideoStampService
    {
        private readonly string _ffmpegPath;
        private readonly ILogger<VideoStampService> _logger;

        public VideoStampService(IConfiguration configuration,
                                 ILogger<VideoStampService> logger)
        {
            _ffmpegPath = configuration["FFmpeg:Path"]
                ?? throw new Exception("FFmpeg path not configured.");
            _logger = logger;
        }

        public async Task<string> StampVideoAsync(
            string inputPath,
            string stampText,
            string qrImagePath)
        {
            var outputPath = Path.Combine(
                Path.GetTempPath(),
                $"stamped_{Guid.NewGuid()}.mp4");

            var escapedText = stampText
                .Replace(":", "\\:")
                .Replace("'", "\\'")
                .Replace("\n", "\\n");

            var fontPath = "C\\\\:/Windows/Fonts/arialbd.ttf";

            var filter = $@"
drawbox=x=0:y=ih-170:w=iw:h=170:color=black@0.85:t=fill,

drawtext=fontfile={fontPath}:
text='{escapedText}':
fontcolor=white:
fontsize=30:
line_spacing=8:
x=(w-text_w)/2:
y=ih-155,

drawtext=fontfile={fontPath}:
text='GPS VERIFIED':
fontcolor=yellow:
fontsize=26:
x=20:
y=20:
enable='mod(t,5)<2',

overlay=W-w-30:H-h-200
";

            var arguments =
                $"-y -i \"{inputPath}\" " +
                $"-i \"{qrImagePath}\" " +
                $"-filter_complex \"{filter}\" " +
                "-c:v libx264 " +
                "-profile:v high " +
                "-preset slow " +
                "-crf 18 " +
                "-pix_fmt yuv420p " +
                "-movflags +faststart " +
                "-c:a aac -b:a 128k " +
                $"\"{outputPath}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = arguments,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            _logger.LogInformation("Starting FFmpeg stamping process...");

            process.Start();

            string error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _logger.LogError("FFmpeg failed: {Error}", error);
                throw new Exception("Video stamping failed.");
            }

            _logger.LogInformation("Video stamping completed successfully.");

            return outputPath;
        }
    }
}