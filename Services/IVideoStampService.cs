namespace GpsMediaStamp.Web.Services;

public interface IVideoStampService
{
    Task<string> StampVideoAsync(string inputPath, string stampText);
}