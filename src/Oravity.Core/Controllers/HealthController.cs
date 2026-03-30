using Microsoft.AspNetCore.Mvc;

namespace Oravity.Core.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        Service = "Oravity.Core",
        Status = "Healthy",
        Timestamp = DateTime.UtcNow,
        Version = "1.0.0"
    });
}
