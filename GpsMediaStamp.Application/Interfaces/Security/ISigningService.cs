using System;

namespace GpsMediaStamp.Application.Interfaces.Security
{
    public interface ISigningService
    {
        string SignHash(string hash);
        bool VerifySignature(string hash, string signature);
        string GetPublicKey();
    }
}