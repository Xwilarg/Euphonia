using Euphonia.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Euphonia.API.Controllers;

[ApiController]
[Route("/api/")]
public class RootController : ControllerBase
{

    private readonly ILogger<RootController> _logger;

    public RootController(ILogger<RootController> logger)
    {
        _logger = logger;
    }

    [HttpGet(Name = "")]
    public IActionResult Get()
    {
        return StatusCode(StatusCodes.Status200OK, new Response()
        {
            Success = true,
            Reason = null
        });
    }
}
