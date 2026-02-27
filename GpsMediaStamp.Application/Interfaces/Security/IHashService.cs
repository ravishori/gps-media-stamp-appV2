using System;

namespace GpsMediaStamp.Application.Interfaces.Security
{
    public interface IHashService
    {
        string GenerateSha256(string filePath);
    }
}