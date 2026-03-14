<<<<<<< HEAD
﻿using Microsoft.AspNetCore.Mvc;

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
=======
﻿using Microsoft.AspNetCore.Mvc;

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
>>>>>>> d177deee08682a00b113e8459cb62227e9e6ddd6
}