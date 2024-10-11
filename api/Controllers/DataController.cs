using Euphonia.API.Models;
using Euphonia.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Euphonia.API.Controllers;

[ApiController]
[Route("/api/data/")]
public class DataController : ControllerBase
{

    private readonly ILogger<RootController> _logger;
    private HttpClient _client;

    public DataController(ILogger<RootController> logger, HttpClient client)
    {
        _logger = logger;
        _client = client;
    }

    [HttpPost("update")]
    [Authorize]
    public Response UpdateSong([FromForm] SongForm data)
    {
        return new()
        {
            Success = true,
            Reason = null
        };
    }

    [HttpPost("upload")]
    [Authorize]
    public Response UploadSong([FromForm]YoutubeForm data)
    {
        var folder = (User.Identity as ClaimsIdentity).FindFirst(x => x.Type == ClaimTypes.UserData).Value;

        // Download album image
        var albumName = data.AlbumName == null ? null : GetAlbumName(data.Artist, data.AlbumName);
        if (albumName != null && !string.IsNullOrWhiteSpace(data.AlbumUrl))
        {
            var img = Image.Load(_client.GetByteArrayAsync(data.AlbumUrl).GetAwaiter().GetResult());
            var imagePath = GetImagePath(folder, albumName, "png");
            img.SaveAsPng(imagePath);
        }

        // Prepare to download the rest
        var musicKey = GetMusicKey(data.Name, data.Artist, data.SongType);
        var rawSongPath = GetRawMusicPath(folder, musicKey);
        var normSongPath = GetNormalizedMusicPath(folder, musicKey);
        var info = Serialization.Deserialize<EuphoniaInfo>(System.IO.File.ReadAllText($"{folder}/info.json"));


        if (System.IO.File.Exists(normSongPath) || System.IO.File.Exists(rawSongPath))
        {
            System.IO.File.Delete(rawSongPath);
            System.IO.File.Delete(normSongPath);
            return new()
            {
                Success = false,
                Reason = "There is already a music saved with the same filename"
            };
        }

        // Download from YouTube and normalize
        ExecuteProcess(new("yt-dlp", $"{data.Youtube} -o \"{rawSongPath}\" -x --audio-format {AudioFormat} -q --progress"));
        ExecuteProcess(new("ffmpeg-normalize", $"\"{rawSongPath}\" -pr -ext {AudioFormat} -o \"{normSongPath}\" -c:a libmp3lame"));

        // Save to json

        // We sanitize user inputs just in case
        var songName = data.Name.Trim();
        var artist = data.Artist?.Trim();
        var songType = data.SongType?.Trim();
        var album = albumName;
        var albumUrl = data.AlbumUrl?.Trim();
        if (string.IsNullOrWhiteSpace(songType)) songType = null;

        // Create output path
        var outMusicPath = GetMusicKey(songName, artist, songType);

        // Format album data
        string albumKey = null;
        var hasAlbum = !string.IsNullOrWhiteSpace(albumUrl);
        if (hasAlbum)
        {
            albumKey = GetAlbumName(artist, album);
        }

        // Create Song class
        var m = new Song
        {
            Album = albumKey,
            Artist = artist,
            Name = songName,
            Path = outMusicPath,
            Playlist = string.IsNullOrWhiteSpace(data.Playlist) ? "default" : data.Playlist.Trim(),
            Source = data.AlbumUrl,
            Type = songType
        };
        info.Musics.Add(m);

        // If album exists we add it to the JSON too
        if (hasAlbum && !info.Albums.ContainsKey(album))
        {
            info.Albums.Add(albumKey, new()
            {
                Name = albumKey,
                Path = $"{albumKey}.png",
                Source = albumUrl
            });
        }

        System.IO.File.WriteAllText($"{folder}/info.json", Serialization.Serialize(info));

        return new()
        {
            Success = true,
            Reason = null
        };
    }

    private const string AudioFormat = "mp3";

    private string CleanPath(string name)
    {
        var forbidden = new[] { '<', '>', ':', '\\', '/', '"', '|', '?', '*', '#', '&', '%' };
        foreach (var c in forbidden)
        {
            name = name.Replace(c.ToString(), string.Empty);
        }
        return name;
    }

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

    private string GetAlbumName(string? artist, string album)
        => $"{CleanPath(artist?.Trim() ?? "unknown")}_{CleanPath(album.Trim())}";

    private string GetSongName(string song, string? artist)
        => $"{CleanPath(song.Trim())}_{CleanPath(artist?.Trim() ?? "unknown")}";

    private string GetImagePath(string dataFolder, string albumName, string ext)
        => $"{dataFolder}/icon/{albumName}.{ext}";

    private string GetRawMusicPath(string dataFolder, string musicKey)
        => $"{dataFolder}/raw/{musicKey}";

    private string GetNormalizedMusicPath(string dataFolder, string musicKey)
        => $"{dataFolder}/normalized/{musicKey}";

    private void ExecuteProcess(ProcessStartInfo startInfo)
    {
        using CancellationTokenSource source = new();

        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.UseShellExecute = false;

        var p = Process.Start(startInfo);
        Task t = Task.Run(async () =>
        {
            for (int i = 0; i < 60000; i += 1000)
            {
                if (source.Token.IsCancellationRequested) return;
                await Task.Delay(1000);
            }
            p.Kill();
        });
        p.Start();

        StringBuilder err = new();

        p.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data)) err.AppendLine(e.Data);
        };
        p.BeginErrorReadLine();
        p.WaitForExit();
        source.Cancel();

        if (p.ExitCode != 0)
        {
            throw new Exception($"{startInfo.FileName} failed: {err}");
        }
    }
}
