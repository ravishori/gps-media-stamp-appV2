using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace GpsMediaStamp.Web.Services
{
    public interface IFileStorageService
    {
        Task<string> SaveRawAsync(IFormFile file);
        Task<string> SaveStampedAsync(string tempFilePath, string fileName);
    }
}