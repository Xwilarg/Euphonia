using Euphonia.API.Models.Data;
using Euphonia.API.Models.Request;
using Euphonia.API.Models.Response;
using Euphonia.API.Services;
using Euphonia.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Web;

namespace Euphonia.API.Controllers;

[ApiController]
[Route("/api/lyrics/")]
public class LyricsController : ControllerBase
{

    private readonly ILogger<RootController> _logger;
    private WebsiteManager _manager;
    private HttpClient _client;

    public LyricsController(ILogger<RootController> logger, WebsiteManager manager, HttpClient client)
    {
        _logger = logger;
        _manager = manager;
        _client = client;
    }

    [HttpPost("get")]
    [Authorize]
    public IActionResult ArchiveSong([FromForm] SongSearchForm data)
    {
        var folder = _manager.GetPath((User.Identity as ClaimsIdentity).FindFirst(x => x.Type == ClaimTypes.UserData).Value);
        var auth = Serialization.Deserialize<Credentials>(System.IO.File.ReadAllText($"{folder}/credentials.json"));

        if (auth.Genius == null)
        {
            return StatusCode(StatusCodes.Status401Unauthorized, new BaseResponse()
            {
                Success = false,
                Reason = "Missing API token"
            });
        }

        using HttpClient http = new(); // TODO: Remove this?
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Genius);
        http.GetStringAsync($"https://api.genius.com/search?q={HttpUtility.UrlEncode($"{data.Name} {data.Artist}")}");

        return StatusCode(StatusCodes.Status200OK, new BaseResponse()
        {
            Success = true,
            Reason = null
        });
    }
}
