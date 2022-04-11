using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MultipleAuthenticationSchemes.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    [HttpGet]
    [Authorize(AuthenticationSchemes = "ApiKey")]
    [Authorize(AuthenticationSchemes = "BasicAuthentication")]
    public IActionResult Get(CancellationToken token)
    {
        return Ok("Success");
    }
}