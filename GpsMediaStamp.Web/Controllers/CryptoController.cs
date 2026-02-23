using GpsMediaStamp.Web.Models;
using GpsMediaStamp.Web.Services;
using Microsoft.AspNetCore.Mvc;

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