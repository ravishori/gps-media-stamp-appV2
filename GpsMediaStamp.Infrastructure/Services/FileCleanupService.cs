using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GpsMediaStamp.Infrastructure.Services
{
    public class FileCleanupService : BackgroundService
    {
        private readonly ILogger<FileCleanupService> _logger;

        private readonly string rawFolder = Path.Combine("storage", "raw");
        private readonly string stampedFolder = Path.Combine("storage", "stamped");

        private readonly TimeSpan fileLifetime = TimeSpan.FromHours(24);
        private readonly TimeSpan cleanupInterval = TimeSpan.FromMinutes(30);

        public FileCleanupService(ILogger<FileCleanupService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("File cleanup service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Only clean raw (temporary upload inputs).
                    // Stamped files are the final deliverable and must persist
                    // until the user explicitly deletes them via the delete API.
                    CleanFolder(rawFolder);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "File cleanup failed.");
                }

                await Task.Delay(cleanupInterval, stoppingToken);
            }
        }

        private void CleanFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return;

            var files = Directory.GetFiles(folderPath);

            foreach (var file in files)
            {
                try
                {
                    var info = new FileInfo(file);

                    if (info.CreationTimeUtc < DateTime.UtcNow - fileLifetime)
                    {
                        File.Delete(file);

                        _logger.LogInformation("Deleted expired file: {File}", file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed deleting file: {File}", file);
                }
            }
        }
    }
}