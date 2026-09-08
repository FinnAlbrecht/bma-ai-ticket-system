using Microsoft.AspNetCore.Mvc;

namespace TicketSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "ok", message = "TicketSystem.Api läuft." });
    }
}
