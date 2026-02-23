using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GpsMediaStamp.Web.Services
{
    public class RsaSigningService : ISigningService
    {
        private readonly RSA _rsa;
        private readonly string _privateKeyPath;

        public RsaSigningService()
        {
            var securityFolder = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "security");

            if (!Directory.Exists(securityFolder))
                Directory.CreateDirectory(securityFolder);

            _privateKeyPath = Path.Combine(securityFolder, "private_key.pem");

            _rsa = RSA.Create(2048);

            LoadOrCreateKey();
        }

        private void LoadOrCreateKey()
        {
            if (File.Exists(_privateKeyPath))
            {
                var privateKeyBytes = File.ReadAllBytes(_privateKeyPath);
                _rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
            }
            else
            {
                var privateKeyBytes = _rsa.ExportRSAPrivateKey();
                File.WriteAllBytes(_privateKeyPath, privateKeyBytes);
            }
        }

        public string SignHash(string hash)
        {
            var hashBytes = Encoding.UTF8.GetBytes(hash);

            var signatureBytes = _rsa.SignData(
                hashBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            return Convert.ToBase64String(signatureBytes);
        }

        public bool VerifySignature(string hash, string signature)
        {
            var hashBytes = Encoding.UTF8.GetBytes(hash);
            var signatureBytes = Convert.FromBase64String(signature);

            return _rsa.VerifyData(
                hashBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
        }

        public string GetPublicKey()
        {
            var publicKeyBytes = _rsa.ExportSubjectPublicKeyInfo();
            return Convert.ToBase64String(publicKeyBytes);
        }
    }
}