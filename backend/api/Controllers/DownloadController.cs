using Euphonia.API.Models.Request;
using Euphonia.API.Models.Request.Upload;
using Euphonia.API.Models.Response;
using Euphonia.API.Services;
using Euphonia.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Euphonia.API.Controllers;

[ApiController]
[Route("/api/download/")]
public class DownloadController : ControllerBase
{

    private readonly ILogger<DownloadController> _logger;
    private WebsiteManager _manager;
    private DownloaderManager _download;
    private HttpClient _client;

    public DownloadController(ILogger<DownloadController> logger, WebsiteManager manager, HttpClient client, DownloaderManager download)
    {
        _logger = logger;
        _manager = manager;
        _client = client;

        _download = download;
    }


    [HttpPost("progress")]
    [Authorize]
    public IActionResult GetProgress()
    {
        var folder = _manager.GetPath((User.Identity as ClaimsIdentity).FindFirst(x => x.Type == ClaimTypes.UserData).Value);

        return StatusCode(StatusCodes.Status200OK, new SongDownloadResponse()
        {
            Success = true,
            Reason = null,
            Data = _download.Get(folder).GetProgress()
        });
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

    [HttpPost("repair")]
    [Authorize]
    public IActionResult RepairSong([FromForm] SongIdentifier data)
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

        if (song.Source == "localfile")
        {
            var rawPath = GetRawMusicPath(folder, song.RawPath);
            var normPath = GetNormalizedMusicPath(folder, song.Path);
            System.IO.File.Delete(normPath);

            _download.Get(folder).QueueToNormalize(song, rawPath, normPath);
        }
        else
        {
            var rawPath = GetRawMusicPath(folder, song.RawPath);
            var normPath = GetNormalizedMusicPath(folder, song.Path);
            System.IO.File.Delete(rawPath);
            System.IO.File.Delete(normPath);

            _download.Get(folder).QueueToDownload(song, song.Source, rawPath, normPath);
        }

        return StatusCode(StatusCodes.Status200OK, new BaseResponse()
        {
            Success = true,
            Reason = null
        });
    }

    private IActionResult UploadSongInternal(AUploadForm data, out string folder, out Song song, out string rawSongPath, out string normSongPath, string extension)
    {
        folder = _manager.GetPath((User.Identity as ClaimsIdentity).FindFirst(x => x.Type == ClaimTypes.UserData).Value);

        // Download album image
        var albumName = data.AlbumName == null ? null : GetAlbumName(data.Artist, data.AlbumName);
        if (albumName != null && !string.IsNullOrWhiteSpace(data.AlbumUrl))
        {
            if (!Utils.SaveUrlAsImage(_client, data.AlbumUrl, GetImagePath(folder, albumName, "webp"), out var error))
            {
                song = null;
                rawSongPath = null;
                normSongPath = null;
                return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
                {
                    Success = false,
                    Reason = $"Image URL is invalid: {error}"
                });
            }
        }

        // Prepare to download the rest
        var rawKey = GetMusicKey(data.Name, data.Artist, data.SongType, extension);
        var normKey = GetMusicKey(data.Name, data.Artist, data.SongType, AudioFormat);

        rawSongPath = GetRawMusicPath(folder, rawKey);
        normSongPath = GetNormalizedMusicPath(folder, normKey);
        var info = Serialization.Deserialize<EuphoniaInfo>(System.IO.File.ReadAllText($"{folder}/info.json"));


        if (System.IO.File.Exists(normSongPath) || System.IO.File.Exists(rawSongPath))
        {
            if (info.Musics.Any(x => x.Name == data.Name.Trim() && x.Artist == data.Artist?.Trim() && data.SongType == data.SongType?.Trim()))
            {
                song = null;
                return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
                {
                    Success = false,
                    Reason = "There is already a music saved with the same filename"
                });
            }
            System.IO.File.Delete(rawSongPath);
            System.IO.File.Delete(normSongPath);
        }

        // Save to json

        info = Serialization.Deserialize<EuphoniaInfo>(System.IO.File.ReadAllText($"{folder}/info.json")); // Load again for concurency issues

        // We sanitize user inputs just in case
        var songName = data.Name.Trim();
        var artist = data.Artist?.Trim();
        var songType = data.SongType?.Trim();
        var album = data.AlbumName?.Trim();
        var albumUrl = data.AlbumUrl?.Trim();
        if (string.IsNullOrWhiteSpace(songType)) songType = null;

        // Format album data
        string? albumKey = null;
        var hasAlbum = !string.IsNullOrWhiteSpace(albumUrl);
        if (hasAlbum)
        {
            albumKey = GetAlbumName(artist, album);
        }

        // Create Song class
        song = new Song
        {
            Key = Guid.NewGuid().ToString(),
            Album = albumKey,
            Artist = artist,
            Name = songName,
            RawPath = rawKey,
            Path = normKey,
            Playlist = string.IsNullOrWhiteSpace(data.Playlist) ? "default" : data.Playlist.Trim(),
            Source = data is YoutubeForm ytForm ? ytForm.Youtube : "localfile",
            Type = songType
        };
        info.Musics.Add(song);

        // If album exists we add it to the JSON too
        if (hasAlbum && !info.Albums.ContainsKey(albumKey))
        {
            info.Albums.Add(albumKey, new()
            {
                Name = album,
                Path = $"{albumKey}.webp",
                Source = albumUrl
            });
        }

        System.IO.File.WriteAllText($"{folder}/info.json", Serialization.Serialize(info));

        return null;
    }

    [HttpPost("upload/youtube")]
    [Authorize]
    public IActionResult UploadSongYoutube([FromForm] YoutubeForm data)
    {
        var sc = UploadSongInternal(data, out string folder, out Song song, out string rawSongPath, out string normSongPath, AudioFormat);

        if (sc != null)
        {
            return sc;
        }

        _download.Get(folder).QueueToDownload(song, data.Youtube, rawSongPath, normSongPath);
        return StatusCode(StatusCodes.Status200OK, new BaseResponse()
        {
            Success = true,
            Reason = null
        });
    }


    [HttpPost("upload/local")]
    [Authorize]
    public async Task<IActionResult> UploadSongLocal([FromForm] LocalFileForm data)
    {
        var sc = UploadSongInternal(data, out string folder, out Song song, out string rawSongPath, out string normSongPath, Path.GetExtension(data.LocalFile.FileName));

        if (sc != null)
        {
            return sc;
        }

        // Save file to disk
        using Stream fileStream = new FileStream(rawSongPath, FileMode.Create);
        await data.LocalFile.CopyToAsync(fileStream);

        // Queue normalization
        _download.Get(folder).QueueToNormalize(song, rawSongPath, normSongPath);

        return StatusCode(StatusCodes.Status200OK, new BaseResponse()
        {
            Success = true,
            Reason = null
        });
    }

    public string AudioFormat => WebsiteDownloaderManager.AudioFormat;

    private string GetMusicKey(string songName, string artist, string songType, string extension)
    {
        var outMusicPath = GetSongName(songName, artist);
        if (!string.IsNullOrWhiteSpace(songType))
        {
            outMusicPath += $"_{songType}";
        }
        outMusicPath += $".{extension}";
        return outMusicPath;
    }

    public static string GetAlbumName(string? artist, string album)
        => $"{Utils.CleanPath(artist?.Trim() ?? "unknown")}_{Utils.CleanPath(album.Trim())}";

    public static string GetSongName(string song, string? artist)
        => $"{Utils.CleanPath(song.Trim())}_{Utils.CleanPath(artist?.Trim() ?? "unknown")}";

    public static string GetImagePath(string dataFolder, string albumName, string ext)
        => $"{dataFolder}icon/{albumName}.{ext}";

    public static string GetRawMusicPath(string dataFolder, string musicKey)
        => $"{dataFolder}raw/{musicKey}";

    public static string GetNormalizedMusicPath(string dataFolder, string musicKey)
        => $"{dataFolder}normalized/{musicKey}";
}
