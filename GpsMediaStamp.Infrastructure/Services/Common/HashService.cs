using GpsMediaStamp.Application.Interfaces.Security;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GpsMediaStamp.Infrastructure.Services.Common
{
    public class HashService : IHashService
    {
        private readonly ILogger<HashService> _logger;

        public HashService(ILogger<HashService> logger)
        {
            _logger = logger;
        }

        public string GenerateSha256(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.");

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found.", filePath);

            try
            {
                using var stream = File.OpenRead(filePath);
                using var sha256 = SHA256.Create();

                var hashBytes = sha256.ComputeHash(stream);

                var builder = new StringBuilder();
                foreach (var b in hashBytes)
                    builder.Append(b.ToString("x2"));

                var hash = builder.ToString();

                _logger.LogInformation("SHA-256 generated for file: {File}", filePath);

                return hash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hash generation failed for file: {File}", filePath);
                throw;
            }
        }
    }
}