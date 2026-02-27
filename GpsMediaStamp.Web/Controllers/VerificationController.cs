using GpsMediaStamp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
///----------------//
using GpsMediaStamp.Application.Interfaces;
using GpsMediaStamp.Application.Interfaces.Common;
using GpsMediaStamp.Application.Interfaces.Image;
using GpsMediaStamp.Application.Interfaces.Security;
using GpsMediaStamp.Application.Interfaces.Qr;
using GpsMediaStamp.Application.Interfaces.Video;

//------------------//
namespace GpsMediaStamp.Web.Controllers
{
    [ApiController]
    [Route("api/verify")]
    public class VerificationController : ControllerBase
    {
        private readonly IVerificationRepository _repository;

        public VerificationController(IVerificationRepository repository)
        {
            _repository = repository;
        }

        [HttpGet("{hash}")]
        public async Task<IActionResult> Verify(string hash)
        {
            var exists = await _repository.ExistsAsync(hash);

            var response = new VerificationResponse
            {
                Hash = hash,
                IsValid = exists,
                Message = exists
                    ? "Hash verified. Video is authentic."
                    : "Hash not found. Video may be tampered."
            };

            return Ok(response);
        }
    }
}