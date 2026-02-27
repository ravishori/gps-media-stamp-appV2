using System.Threading.Tasks;

namespace GpsMediaStamp.Application.Interfaces.Video
{
    public interface IVideoStampService
    {
        Task<string> StampVideoAsync(
            string inputPath,
            string stampText,
            string qrImagePath);
    }
}