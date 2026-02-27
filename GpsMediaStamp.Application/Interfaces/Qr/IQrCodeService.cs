using System.Threading.Tasks;

namespace GpsMediaStamp.Application.Interfaces.Qr
{
    public interface IQrCodeService
    {
        Task<string> GenerateQrAsync(string content);
    }
}