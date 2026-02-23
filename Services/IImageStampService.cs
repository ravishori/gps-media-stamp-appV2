namespace GpsMediaStamp.Web.Services;

public interface IImageStampService
{
    Task<string> StampImageAsync(string inputPath, string stampText);
}