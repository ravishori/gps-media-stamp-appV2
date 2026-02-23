using System;
using System.IO;
using System.Security.Cryptography;

namespace GpsMediaStamp.Web.Services
{
    public class HashService : IHashService
    {
        public string GenerateSha256(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found for hashing.");

            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);

            var hashBytes = sha256.ComputeHash(stream);

            return BitConverter.ToString(hashBytes)
                .Replace("-", "")
                .ToLowerInvariant();
        }
    }
}