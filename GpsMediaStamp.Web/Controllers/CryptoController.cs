using GpsMediaStamp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using GpsMediaStamp.Application.Interfaces.Common;
using GpsMediaStamp.Application.Interfaces.Image;
using GpsMediaStamp.Application.Interfaces.Security;
using GpsMediaStamp.Application.Interfaces;
using GpsMediaStamp.Application.Interfaces.Qr;

namespace GpsMediaStamp.Web.Controllers
{
    [ApiController]
    [Route("api/crypto")]
    public class CryptoController : ControllerBase
    {
        private readonly ISigningService _signingService;

        public CryptoController(ISigningService signingService)
        {
            _signingService = signingService;
        }

        [HttpGet("public-key")]
        public IActionResult GetPublicKey()
        {
            var response = new PublicKeyResponse
            {
                PublicKey = _signingService.GetPublicKey()
            };

            return Ok(response);
        }
    }
}