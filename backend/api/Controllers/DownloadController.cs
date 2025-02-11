using Euphonia.API.Models.Data;
using Euphonia.API.Models.Request;
using Euphonia.API.Models.Response;
using Euphonia.API.Services;
using Euphonia.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace Euphonia.API.Controllers;

[ApiController]
[Route("/api/download/")]
public class DownloadController : ControllerBase
{

    private readonly ILogger<DownloadController> _logger;
    private WebsiteManager _manager;
    private HttpClient _client;

    private Thread _downloadThread;
    ConcurrentQueue<DownloadSongData> _downloadData = new();
    ConcurrentQueue<DownloadSongData> _erroredData = new();

    public DownloadController(ILogger<DownloadController> logger, WebsiteManager manager, HttpClient client)
    {
        _logger = logger;
        _manager = manager;
        _client = client;

        _downloadThread = new(new ThreadStart(DownloadThread));
        _downloadThread.Start();
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

    private string DownloadSong(DownloadSongData data)
    {
        int code; string err;
        Utils.ExecuteProcess(new("yt-dlp", $"{data.DownloadUrl} -o \"{data.RawPath}\" -x --audio-format {AudioFormat} -q --progress"), out code, out err);
        if (code != 0)
        {
            return $"yt-dlp {data.DownloadUrl} -o \"{data.RawPath}\" -x --audio-format {AudioFormat} -q --progress failed:\n{string.Join("", err.TakeLast(1000))}";
        }
        data.CurrentState = DownloadState.Normalizing;
        Utils.ExecuteProcess(new("ffmpeg-normalize", $"\"{data.RawPath}\" -pr -ext {AudioFormat} -o \"{data.NormPath}\" -c:a libmp3lame"), out code, out err);
        if (code != 0)
        {
            return $"ffmpeg-normalize \"{data.RawPath}\" -pr -ext {AudioFormat} -o \"{data.NormPath}\" -c:a libmp3lame failed:\n{string.Join("", err.TakeLast(1000))}";
        }
        return null;
    }

    public void QueueToDownload(Song song, string url, string rawPath, string normPath)
    {
        _downloadData.Enqueue(new()
        {
            Song = song,
            CurrentState = DownloadState.Downloading,
            Error = null,
            RawPath = rawPath,
            NormPath = normPath,
            DownloadUrl = url
        });
    }

    private void DownloadThread()
    {
        while (Thread.CurrentThread.IsAlive)
        {
            if (_downloadData.TryDequeue(out var res))
            {
                var error = DownloadSong(res);
                res.Error = error;
                res.CurrentState = DownloadState.Finished;

                if (res.Error != null)
                {
                    _erroredData.Enqueue(res);
                }
            }
            Thread.Sleep(100);
        }
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

        if (!song.Source.StartsWith("http"))
        {
            return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
            {
                Success = false,
                Reason = "Song doens't have a valid source"
            });
        }

        var rawPath = GetRawMusicPath(folder, song.Path);
        var normPath = GetNormalizedMusicPath(folder, song.Path);
        System.IO.File.Delete(rawPath);
        System.IO.File.Delete(normPath);

        QueueToDownload(song, song.Source, rawPath, normPath);

        return StatusCode(StatusCodes.Status200OK, new BaseResponse()
        {
            Success = true,
            Reason = null
        });
    }

    [HttpPost("upload")]
    [Authorize]
    public IActionResult UploadSong([FromForm] YoutubeForm data)
    {
        var folder = _manager.GetPath((User.Identity as ClaimsIdentity).FindFirst(x => x.Type == ClaimTypes.UserData).Value);

        // Download album image
        var albumName = data.AlbumName == null ? null : GetAlbumName(data.Artist, data.AlbumName);
        if (albumName != null && !string.IsNullOrWhiteSpace(data.AlbumUrl))
        {
            if (!Utils.SaveUrlAsImage(_client, data.AlbumUrl, GetImagePath(folder, albumName, "webp")))
            {
                return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
                {
                    Success = false,
                    Reason = "Image URL is invalid"
                });
            }
        }

        // Prepare to download the rest
        var musicKey = GetMusicKey(data.Name, data.Artist, data.SongType);
        var rawSongPath = GetRawMusicPath(folder, musicKey);
        var normSongPath = GetNormalizedMusicPath(folder, musicKey);
        var info = Serialization.Deserialize<EuphoniaInfo>(System.IO.File.ReadAllText($"{folder}/info.json"));


        if (System.IO.File.Exists(normSongPath) || System.IO.File.Exists(rawSongPath))
        {
            if (info.Musics.Any(x => x.Name == data.Name.Trim() && x.Artist == data.Artist?.Trim() && data.SongType == data.SongType?.Trim()))
            {
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

        // Create output path
        var outMusicPath = GetMusicKey(songName, artist, songType);

        // Format album data
        string? albumKey = null;
        var hasAlbum = !string.IsNullOrWhiteSpace(albumUrl);
        if (hasAlbum)
        {
            albumKey = GetAlbumName(artist, album);
        }

        // Create Song class
        var m = new Song
        {
            Key = Guid.NewGuid().ToString(),
            Album = albumKey,
            Artist = artist,
            Name = songName,
            Path = outMusicPath,
            Playlist = string.IsNullOrWhiteSpace(data.Playlist) ? "default" : data.Playlist.Trim(),
            Source = data.Youtube,
            Type = songType
        };
        info.Musics.Add(m);

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


        QueueToDownload(m, data.Youtube, rawSongPath, normSongPath);

        return StatusCode(StatusCodes.Status200OK, new BaseResponse()
        {
            Success = true,
            Reason = null
        });
    }

    private const string AudioFormat = "mp3";

    private string GetMusicKey(string songName, string artist, string songType)
    {
        var outMusicPath = GetSongName(songName, artist);
        if (!string.IsNullOrWhiteSpace(songType))
        {
            outMusicPath += $"_{songType}";
        }
        outMusicPath += $".{AudioFormat}";
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
