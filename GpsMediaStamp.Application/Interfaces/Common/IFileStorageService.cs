namespace GpsMediaStamp.Application.Interfaces.Common
{

    public interface IFileStorageService
    {
        Task<string> SaveRawAsync(Stream fileStream, string fileName);
        Task<string> SaveStampedAsync(Stream fileStream, string fileName);
    }
}