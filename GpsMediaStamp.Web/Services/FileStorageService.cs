using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GpsMediaStamp.Web.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _rawPath;
        private readonly string _stampedPath;

        public FileStorageService(IWebHostEnvironment env)
        {
            var rootPath = Path.Combine(env.ContentRootPath, "storage");

            _rawPath = Path.Combine(rootPath, "raw");
            _stampedPath = Path.Combine(rootPath, "stamped");

            EnsureDirectories();
        }

        private void EnsureDirectories()
        {
            if (!Directory.Exists(_rawPath))
                Directory.CreateDirectory(_rawPath);

            if (!Directory.Exists(_stampedPath))
                Directory.CreateDirectory(_stampedPath);
        }

        public async Task<string> SaveRawAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file.");

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(_rawPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fullPath;
        }

        public async Task<string> SaveStampedAsync(string tempFilePath, string fileName)
        {
            if (!File.Exists(tempFilePath))
                throw new FileNotFoundException("Stamped file not found.");

            var extension = Path.GetExtension(fileName);
            var finalName = $"{Guid.NewGuid()}{extension}";
            var finalPath = Path.Combine(_stampedPath, finalName);

            await Task.Run(() => File.Move(tempFilePath, finalPath));

            return finalPath;
        }
    }
}