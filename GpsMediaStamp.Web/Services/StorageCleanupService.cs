using GpsMediaStamp.Web.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using System.Text;

namespace GpsMediaStamp.Web.Services
{
    public class StorageCleanupService : BackgroundService
    {
        private readonly ILogger<StorageCleanupService> _logger;
        private readonly StorageCleanupSettings _settings;
        private readonly EmailService _emailService;
        private readonly IWebHostEnvironment _env;

        public StorageCleanupService(
            ILogger<StorageCleanupService> logger,
            IOptions<StorageCleanupSettings> settings,
            EmailService emailService,
            IWebHostEnvironment env)
        {
            _logger = logger;
            _settings = settings.Value;
            _emailService = emailService;
            _env = env;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Storage Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                var report = new StringBuilder();
                bool success = true;

                try
                {
                    string logsPath    = Path.Combine(
                        _env.ContentRootPath,
                        _settings.LogsFolder);

                    string tempPath    = Path.Combine(
                        _env.ContentRootPath,
                        _settings.TempFolder);

                    // Stamped files are the final server-side output.
                    // They are cleaned up after RetentionHours (default 24 h)
                    // to prevent unbounded disk growth. Flutter devices have
                    // already downloaded and saved their own copies by then.
                    string stampedPath = Path.Combine(
                        _env.ContentRootPath,
                        _settings.StampedFolder);

                    int logsDeleted    = DeleteOldFiles(logsPath);
                    int tempDeleted    = DeleteOldFiles(tempPath);
                    int stampedDeleted = DeleteOldFiles(stampedPath);

                    report.AppendLine($"Logs deleted: {logsDeleted}");
                    report.AppendLine($"Temp files deleted: {tempDeleted}");
                    report.AppendLine($"Stamped files deleted: {stampedDeleted}");

                    _logger.LogInformation(report.ToString());
                }
                catch (Exception ex)
                {
                    success = false;

                    report.AppendLine("ERROR OCCURRED");
                    report.AppendLine(ex.ToString());

                    _logger.LogError(ex, "Storage cleanup failed");
                }

                try
                {
                    string subject = success
                        ? "GpsMediaStamp Cleanup Success"
                        : "GpsMediaStamp Cleanup FAILED";

                    await _emailService.SendEmailAsync(subject, report.ToString());
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send cleanup email");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private int DeleteOldFiles(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                _logger.LogWarning($"Directory not found: {directoryPath}");
                return 0;
            }

            int deletedCount = 0;

            var files = Directory.GetFiles(directoryPath);

            foreach (var file in files)
            {
                try
                {
                    var creationTime = File.GetCreationTimeUtc(file);

                    if (DateTime.UtcNow - creationTime >
                        TimeSpan.FromHours(_settings.RetentionHours))
                    {
                        File.Delete(file);
                        deletedCount++;

                        _logger.LogInformation($"Deleted file: {file}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error deleting file: {file}");
                }
            }

            return deletedCount;
        }
    }
}