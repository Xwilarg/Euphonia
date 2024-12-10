using Euphonia.API.Models;
using Euphonia.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

    [HttpPost("integrity")]
    [Authorize]
    public IActionResult Integrity()
    {
        var folder = (User.Identity as ClaimsIdentity).FindFirst(x => x.Type == ClaimTypes.UserData).Value;
        if (!System.IO.File.Exists($"{folder}/info.json"))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new Response()
            {
                Success = false,
                Reason = "info.json not found"
            });
        }
        Utils.ExecuteProcess(new("yt-dlp", $"--version"), out var code, out _);
        if (code != 0)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new Response()
            {
                Success = false,
                Reason = "yt-dlp not found"
            });
        }
        Utils.ExecuteProcess(new("ffmpeg-normalize", $"--version"), out code, out _);
        if (code != 0)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new Response()
            {
                Success = false,
                Reason = "ffmpeg-normalize not found"
            });
        }
        return StatusCode(StatusCodes.Status200OK, new Response()
        {
            Success = true,
            Reason = null
        });
    }
}
