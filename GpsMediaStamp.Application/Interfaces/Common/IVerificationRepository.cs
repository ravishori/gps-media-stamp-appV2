using System.Threading.Tasks;

namespace GpsMediaStamp.Application.Interfaces.Common
{
    public interface IVerificationRepository
    {
        Task SaveAsync(string rawHash, string stampedHash);
        Task<bool> ExistsAsync(string hash);
    }
}