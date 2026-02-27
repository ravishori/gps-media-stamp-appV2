namespace GpsMediaStamp.Application.Interfaces.Image
{
    public interface IImageStampService
    {
        Task<string> StampPremiumImageAsync(
            string inputPath,
            string address,
            double latitude,
            double longitude,
            DateTime timestamp,
            string qrPath);
    }
}