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

    [HttpGet("")]
    public IActionResult Get()
    {
        return StatusCode(StatusCodes.Status200OK, new Response()
        {
            Success = true,
            Reason = null
        });
    }

    [HttpGet("integrity")]
    public IActionResult Integrity()
    {
        Utils.ExecuteProcess(new("yt-dlp", $"--version"), out var code, out _);
        if (code != 0)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new Response()
            {
                Success = false,
                Reason = null
            });
        }
        return StatusCode(StatusCodes.Status200OK, new Response()
        {
            Success = true,
            Reason = null
        });
    }
}
