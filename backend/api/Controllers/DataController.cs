using Euphonia.API.Models.Request;
using Euphonia.API.Models.Response;
using Euphonia.API.Services;
using Euphonia.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Euphonia.API.Controllers;

[ApiController]
[Route("/api/data/")]
public class DataController : ControllerBase
{

    private readonly ILogger<DataController> _logger;
    private WebsiteManager _manager;
    private HttpClient _client;

    public DataController(ILogger<DataController> logger, WebsiteManager manager, HttpClient client)
    {
        _logger = logger;
        _manager = manager;
        _client = client;
    }

    private Song? LookupSong(string key, out string folder, out EuphoniaInfo info)
    {
        folder = _manager.GetPath((User.Identity as ClaimsIdentity)!.FindFirst(x => x.Type == ClaimTypes.UserData)!.Value)!;
        info = Serialization.Deserialize<EuphoniaInfo>(System.IO.File.ReadAllText($"{folder}/info.json"));

        // ID Lookup
        Song? song = info.Musics.FirstOrDefault(x => x.Key == key);

        // Key lookup
        song ??= info.Musics.FirstOrDefault(x => $"{x.Name}_{x.Artist}_{x.Type}" == key);

        return song;
    }

    [HttpPost("favorite")]
    [Authorize]
    public IActionResult FavoriteSong([FromForm] SongToggleAction data)
    {
        var song = LookupSong(data.Key, out var folder, out var info);

        if (song == null)
        {
            return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
            {
                Success = false,
                Reason = "Can't find a song with the given key"
            });
        }

        song.IsFavorite = data.IsOn;

        System.IO.File.WriteAllText($"{folder}/info.json", Serialization.Serialize(info));

        return StatusCode(StatusCodes.Status200OK, new BaseResponse()
        {
            Success = true,
            Reason = null
        });
    }

    [HttpPost("archive")]
    [Authorize]
    public IActionResult ArchiveSong([FromForm] SongIdentifier data)
    {
        var song = LookupSong(data.Key, out var folder, out var info);

        if (song == null)
        {
            return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
            {
                Success = false,
                Reason = "Can't find a song with the given key"
            });
        }

        song.IsArchived = true;

        System.IO.File.WriteAllText($"{folder}/info.json", Serialization.Serialize(info));

        return StatusCode(StatusCodes.Status200OK, new BaseResponse()
        {
            Success = true,
            Reason = null
        });
    }

    [HttpPatch("playlist")]
    [Authorize]
    public IActionResult UpdateSongPlaylists([FromForm] SongFormPatchPlaylist data)
    {
        var song = LookupSong(data.Key, out var folder, out var info);

        if (song == null)
        {
            return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
            {
                Success = false,
                Reason = "Can't find a song with the given key"
            });
        }

        if (data.Playlists != null) song.Playlists = data.Playlists;

        System.IO.File.WriteAllText($"{folder}/info.json", Serialization.Serialize(info));

        return StatusCode(StatusCodes.Status204NoContent);
    }

    [HttpPost("update")]
    [Authorize]
    public IActionResult UpdateSong([FromForm] SongForm data)
    {
        var song = LookupSong(data.Key, out var folder, out var info);

        if (song == null)
        {
            return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
            {
                Success = false,
                Reason = "Can't find a song with the given key"
            });
        }

        song.Tags = data.Tags ?? [];
        song.Source = data.Source;
        song.Name = data.Name;
        song.Artist = data.Artist;
        if (data.Playlists != null) song.Playlists = data.Playlists;

        if (data.CoverUrl == null) // No album name, no URL, we have no album
        {
            song.ThumbnailHash = null;
        }
        else
        {
            var hash = Utils.Sha256(data.CoverUrl);
            song.ThumbnailHash = hash;
            if (!info.AlbumHashes.ContainsKey(hash)) // Image not downloaded yet
            {
                if (!Utils.SaveUrlAsImage(_client, data.CoverUrl, DownloadController.GetImagePath(folder, hash, "webp"), out var error))
                {
                    return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
                    {
                        Success = false,
                        Reason = $"Image URL is invalid: {error}"
                    });
                }
                info.AlbumHashes.Add(hash, $"{hash}.webp");
            }
        }

        System.IO.File.WriteAllText($"{folder}/info.json", Serialization.Serialize(info));

        return StatusCode(StatusCodes.Status200OK, new SongResponse()
        {
            Success = true,
            Reason = null,

            Name = song.Name,
            Artist = song.Artist,
            Tags = song.Tags ?? [],

            Thumnail = song.ThumbnailHash
        });
    }
}
