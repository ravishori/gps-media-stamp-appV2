using System.Threading.Tasks;

namespace GpsMediaStamp.Application.Interfaces.Security
{
    public interface IEmailAlertService
    {
        Task SendAsync(string subject, string body);
    }
}