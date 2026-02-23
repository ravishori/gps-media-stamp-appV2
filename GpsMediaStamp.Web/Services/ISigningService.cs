using System;

namespace GpsMediaStamp.Web.Services
{
    public interface ISigningService
    {
        string SignHash(string hash);
        bool VerifySignature(string hash, string signature);
        string GetPublicKey();
    }
}