using Microsoft.AspNetCore.Mvc;

namespace GeoStampApi.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok("GeoStamp API is running 🚀");
    }
}