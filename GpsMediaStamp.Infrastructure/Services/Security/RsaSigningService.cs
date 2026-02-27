using GpsMediaStamp.Application.Interfaces.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GpsMediaStamp.Infrastructure.Services.Security
{
    public class RsaSigningService : ISigningService
    {
        private readonly string _privateKeyPath;
        private readonly string _publicKeyPath;
        private readonly ILogger<RsaSigningService> _logger;

        public RsaSigningService(
            IWebHostEnvironment env,
            ILogger<RsaSigningService> logger)
        {
            var keyFolder = Path.Combine(env.ContentRootPath, "keys");
            Directory.CreateDirectory(keyFolder);

            _privateKeyPath = Path.Combine(keyFolder, "private_key.pem");
            _publicKeyPath = Path.Combine(keyFolder, "public_key.pem");

            _logger = logger;

            EnsureKeysExist();
        }

        private void EnsureKeysExist()
        {
            if (File.Exists(_privateKeyPath) && File.Exists(_publicKeyPath))
                return;

            using var rsa = RSA.Create(2048);

            var privateKey = rsa.ExportRSAPrivateKey();
            var publicKey = rsa.ExportRSAPublicKey();

            File.WriteAllText(_privateKeyPath,
                WritePem("RSA PRIVATE KEY", privateKey));

            File.WriteAllText(_publicKeyPath,
                WritePem("RSA PUBLIC KEY", publicKey));

            _logger.LogInformation("RSA key pair generated.");
        }

        public string SignHash(string hash)
        {
            var hashBytes = Encoding.UTF8.GetBytes(hash);

            using var rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(ReadPem(_privateKeyPath), out _);

            var signature = rsa.SignData(
                hashBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            return Convert.ToBase64String(signature);
        }

        public bool VerifySignature(string hash, string signature)
        {
            var hashBytes = Encoding.UTF8.GetBytes(hash);
            var signatureBytes = Convert.FromBase64String(signature);

            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(ReadPem(_publicKeyPath), out _);

            return rsa.VerifyData(
                hashBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
        }

        public string GetPublicKey()
        {
            return File.ReadAllText(_publicKeyPath);
        }

        private static byte[] ReadPem(string path)
        {
            var pem = File.ReadAllText(path);

            var lines = pem
                .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
                .Replace("-----END RSA PRIVATE KEY-----", "")
                .Replace("-----BEGIN RSA PUBLIC KEY-----", "")
                .Replace("-----END RSA PUBLIC KEY-----", "")
                .Replace("\r", "")
                .Replace("\n", "");

            return Convert.FromBase64String(lines);
        }

        private static string WritePem(string title, byte[] data)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"-----BEGIN {title}-----");

            var base64 = Convert.ToBase64String(data);
            for (int i = 0; i < base64.Length; i += 64)
                builder.AppendLine(base64.Substring(i, Math.Min(64, base64.Length - i)));

            builder.AppendLine($"-----END {title}-----");

            return builder.ToString();
        }
    }
}