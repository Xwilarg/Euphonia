using Euphonia.API.Models.Request;
using Euphonia.API.Models.Response;
using Euphonia.API.Services;
using Euphonia.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Web;

namespace Euphonia.API.Controllers;

[ApiController]
[Route("/api/playlist/")]
public class PlaylistController : ControllerBase
{

    private readonly ILogger<PlaylistController> _logger;
    private WebsiteManager _manager;
    private HttpClient _client;

    public PlaylistController(ILogger<PlaylistController> logger, WebsiteManager manager, HttpClient client)
    {
        _logger = logger;
        _manager = manager;
        _client = client;
    }

    [HttpPost("add")]
    [Authorize]
    public IActionResult CreatePlaylist([FromForm] PlaylistForm data)
    {
        var folder = _manager.GetPath((User.Identity as ClaimsIdentity).FindFirst(x => x.Type == ClaimTypes.UserData).Value);
        var info = Serialization.Deserialize<EuphoniaInfo>(System.IO.File.ReadAllText($"{folder}/info.json"));

        var key = HttpUtility.UrlEncode(data.Name);

        if (info.Playlists.Any(x => x.Key == key))
        {
            return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
            {
                Success = false,
                Reason = "There is already a playlist with that name"
            });
        }

        if (data.ImageUrl != null)
        {
            if (!Utils.SaveUrlAsImage(_client, data.ImageUrl, $"{folder}icon/playlist/{Utils.CleanPath(data.Name)}.webp", out var error))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
                {
                    Success = false,
                    Reason = $"Image URL is invalid: {error}"
                });
            }
        }

        info.Playlists.Add(key, new()
        {
            Name = (data.FullName ?? data.Name).ToLowerInvariant(),
            Description = data.Description,
            ImageUrl = data.ImageUrl == null ? null : $"{Utils.CleanPath(data.Name)}.webp"
        });

        System.IO.File.WriteAllText($"{folder}/info.json", Serialization.Serialize(info));

        return StatusCode(StatusCodes.Status200OK, new BaseResponse()
        {
            Success = true,
            Reason = null
        });
    }
}
