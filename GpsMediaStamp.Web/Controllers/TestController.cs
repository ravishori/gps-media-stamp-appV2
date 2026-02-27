using GpsMediaStamp.Application.Interfaces.Security;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace GpsMediaStamp.Web.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        private readonly IEmailAlertService _emailService;

        public TestController(IEmailAlertService emailService)
        {
            _emailService = emailService;
        }

        [HttpGet("email")]
        public async Task<IActionResult> TestEmail()
        {
            await _emailService.SendAsync(
                "Test Email - GpsMediaStamp",
                $"Email triggered at {DateTime.Now}");

            return Ok("Email triggered successfully.");
        }
    }
}