
using Microsoft.AspNetCore.Mvc;

namespace PayClaim.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    [HttpGet("exception")]
    public IActionResult ThrowException()
    {
        throw new Exception("This is a test exception.");
    }
}
