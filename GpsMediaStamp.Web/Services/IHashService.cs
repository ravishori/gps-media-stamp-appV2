using System;

namespace GpsMediaStamp.Web.Services
{
    public interface IHashService
    {
        string GenerateSha256(string filePath);
    }
}