using System.Threading.Tasks;

namespace GpsMediaStamp.Web.Services
{
    public interface IQrCodeService
    {
        Task<string> GenerateQrAsync(string content);
    }
}