using Euphonia.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Euphonia.API.Controllers;

[ApiController]
[Route("/api/data/")]
public class DataController : ControllerBase
{

    private readonly ILogger<RootController> _logger;

    public DataController(ILogger<RootController> logger)
    {
        _logger = logger;
    }

    [HttpPost("upload")]
    [Authorize]
    public Response UploadSong([FromForm]YoutubeForm data)
    {
        var folder = (User.Identity as ClaimsIdentity).FindFirst(x => x.Type == ClaimTypes.UserData).Value;

        var albumName = data.AlbumName == null ? null : GetAlbumName(data.Artist, data.AlbumName);
        var imagePath = GetImagePath(folder, albumName);

        return new()
        {
            Success = true,
            Reason = null
        };
    }

    private string CleanPath(string name)
    {
        var forbidden = new[] { '<', '>', ':', '\\', '/', '"', '|', '?', '*', '#', '&', '%' };
        foreach (var c in forbidden)
        {
            name = name.Replace(c.ToString(), string.Empty);
        }
        return name;
    }

    private string GetAlbumName(string? artist, string album)
        => $"{CleanPath(artist?.Trim() ?? "unknown")}_{CleanPath(album.Trim())}";

    private string GetSongName(string song, string? artist)
        => $"{CleanPath(song.Trim())}_{CleanPath(artist?.Trim() ?? "unknown")}";

    private string GetImagePath(string dataFolder, string albumName)
        => $"{dataFolder}/icon/{albumName}.png";

    private string GetRawMusicPath(string dataFolder, string musicKey)
        => $"{dataFolder}/raw/{musicKey}";

    private string GetNormalizedMusicPath(string dataFolder, string musicKey)
        => $"{dataFolder}/normalized/{musicKey}";
}
