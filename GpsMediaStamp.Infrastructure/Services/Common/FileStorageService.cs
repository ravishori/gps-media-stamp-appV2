using GpsMediaStamp.Application.Interfaces.Common;
using Microsoft.AspNetCore.Hosting;

namespace GpsMediaStamp.Infrastructure.Services.Common;

public class FileStorageService : IFileStorageService
{
    private readonly string _rawPath;
    private readonly string _stampedPath;
    private readonly IWebHostEnvironment _env;
    public FileStorageService(IWebHostEnvironment env)
    {
        var rootPath = Path.Combine(env.ContentRootPath, "storage");
        _env = env;

        _rawPath = Path.Combine(rootPath, "raw");
        _stampedPath = Path.Combine(rootPath, "stamped");

        EnsureDirectories();
    }

    private void EnsureDirectories()
    {
        Directory.CreateDirectory(_rawPath);
        Directory.CreateDirectory(_stampedPath);
    }

    public async Task<string> SaveRawAsync(Stream fileStream, string fileName)
    {
        var path = Path.Combine(_rawPath, fileName);

        using var output = new FileStream(path, FileMode.Create);
        await fileStream.CopyToAsync(output);

        return path;
    }

    public async Task<string> SaveStampedAsync(Stream stream, string originalFileName)
    {
        var stampedFolder = Path.Combine(
            _env.ContentRootPath,
            "storage",
            "stamped");

        Directory.CreateDirectory(stampedFolder);

        string uniqueName =
            $"{Path.GetFileNameWithoutExtension(originalFileName)}_" +
            $"{DateTime.UtcNow:yyyyMMddHHmmssfff}.mp4";

        var filePath = Path.Combine(stampedFolder, uniqueName);

        using var fileStream = new FileStream(
            filePath,
            FileMode.CreateNew,   // 🔥 important
            FileAccess.Write);

        await stream.CopyToAsync(fileStream);

        return filePath;
    }
}