using Euphonia.API.Models.Request;
using Euphonia.API.Models.Response;
using Euphonia.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Web;

namespace Euphonia.API.Controllers;

[ApiController]
[Route("/api/playlist/")]
public class PlaylistController : ControllerBase
{

    private readonly ILogger<PlaylistController> _logger;
    private HttpClient _client;

    public PlaylistController(ILogger<PlaylistController> logger, HttpClient client)
    {
        _logger = logger;
        _client = client;
    }

    [HttpDelete("remove")]
    [Authorize]
    public IActionResult RemovePlaylist([FromForm] SongIdentifier form)
    {
        var info = Serialization.Deserialize<EuphoniaInfo>(System.IO.File.ReadAllText($"/data/info.json"));

        var key = HttpUtility.UrlEncode(form.Key.Replace(" ", "").ToLowerInvariant());

        if (!info.Playlists.Any(x => x.Key == key))
        {
            return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
            {
                Success = false,
                Reason = "There is no playlist with that name"
            });
        }

        info.Playlists.Remove(key);

        System.IO.File.WriteAllText($"/data/info.json", Serialization.Serialize(info));

        return StatusCode(StatusCodes.Status200OK, new BaseResponse()
        {
            Success = true,
            Reason = null
        });
    }

    [HttpPost("add")]
    [Authorize]
    public IActionResult CreatePlaylist([FromForm] PlaylistForm data)
    {
        var info = Serialization.Deserialize<EuphoniaInfo>(System.IO.File.ReadAllText($"/data/info.json"));

        var key = HttpUtility.UrlEncode(data.Name.Replace(" ", "").ToLowerInvariant());

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
            if (!Utils.SaveUrlAsImage(_client, data.ImageUrl, $"/datA/icon/playlist/{Utils.CleanPath(data.Name)}.webp", out var error))
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
            Name = data.FullName ?? data.Name,
            Description = data.Description,
            ImageUrl = data.ImageUrl == null ? null : $"{Utils.CleanPath(data.Name)}.webp"
        });

        System.IO.File.WriteAllText($"/data/info.json", Serialization.Serialize(info));

        return StatusCode(StatusCodes.Status200OK, new BaseResponse()
        {
            Success = true,
            Reason = null
        });
    }
}
