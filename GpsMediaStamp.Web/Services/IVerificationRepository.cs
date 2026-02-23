using System.Threading.Tasks;

namespace GpsMediaStamp.Web.Services
{
    public interface IVerificationRepository
    {
        Task SaveAsync(string rawHash, string stampedHash);
        Task<bool> ExistsAsync(string hash);
    }
}