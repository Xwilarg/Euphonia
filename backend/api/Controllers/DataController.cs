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
        folder = _manager.GetPath((User.Identity as ClaimsIdentity).FindFirst(x => x.Type == ClaimTypes.UserData).Value);
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

        song.Tags = data.Tags;
        song.Source = data.Source;
        song.Name = data.Name;
        song.Artist = data.Artist;
        if (data.Playlists != null) song.Playlists = data.Playlists;

        Album albumData;

        if (data.AlbumUrl == null && data.AlbumName == null) // No album name, no URL, we have no album
        {
            song.Album = string.IsNullOrWhiteSpace(data.AlbumKey) ? null : data.AlbumKey; // In case we only updated the key
            albumData = null;
        }
        else
        {
            var albName = string.IsNullOrWhiteSpace(data.AlbumName) ? null : data.AlbumName;
            string key;
            if (string.IsNullOrWhiteSpace(data.AlbumKey))
            {
                key = song.Album != null && info.Albums.ContainsKey(song.Album)
                        ? song.Album // The album already exists, so we use it
                        : DownloadController.GetAlbumName(song.Artist, albName ?? Guid.NewGuid().ToString()); // Else we generate one
            }
            else
            {
                key = data.AlbumKey;
            }
            song.Album = Utils.CleanPath(key);

            var source = string.IsNullOrWhiteSpace(data.AlbumUrl) ? null : data.AlbumUrl;
            if (!info.Albums.ContainsKey(key)) // New album we don't have before, let's add it
            {
                albumData = new()
                {
                    Name = string.IsNullOrWhiteSpace(data.AlbumName) ? null : data.AlbumName,
                    Path = source == null ? null : $"{key}.webp",
                    Source = source
                };
                info.Albums.Add(key, albumData);
                if (source != null) // Album have a source, we try to download the image
                {
                    if (!Utils.SaveUrlAsImage(_client, source, DownloadController.GetImagePath(folder, key, "webp"), out var error))
                    {
                        return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
                        {
                            Success = false,
                            Reason = $"Image URL is invalid: {error}"
                        });
                    }
                }
            }
            else // Album already exists
            {
                if (info.Albums[key].Source != null) // We only attempt to update sources if one was provided
                {
                    info.Albums[key].Path = source == null ? null : $"{key}.webp";

                    var oldSource = info.Albums[key].Source;
                    info.Albums[key].Source = source;
                    if (source != null && oldSource != source) // Source image changed
                    {
                        if (!Utils.SaveUrlAsImage(_client, source, DownloadController.GetImagePath(folder, key, "webp"), out var error))
                        {
                            return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
                            {
                                Success = false,
                                Reason = $"Image URL is invalid: {error}"
                            });
                        }
                    }
                }
                if (data.AlbumName != null) // Was name provided?
                {
                    info.Albums[key].Name = string.IsNullOrWhiteSpace(data.AlbumName) ? null : data.AlbumName;
                }
                albumData = info.Albums[key];
            }
        }

        System.IO.File.WriteAllText($"{folder}/info.json", Serialization.Serialize(info));

        return StatusCode(StatusCodes.Status200OK, new SongResponse()
        {
            Success = true,
            Reason = null,

            Name = song.Name,
            Artist = song.Artist,
            Tags = song.Tags,

            AlbumKey = song.Album,
            AlbumName = albumData?.Name,
            AlbumSource = albumData?.Source,
            AlbumPath = albumData?.Path
        });
    }
}
