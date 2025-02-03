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
        if (data.Playlist != null) song.Playlist = data.Playlist;

        Album albumData;

        if (data.AlbumUrl == null && data.AlbumName == null) // No album name, no URL, we have no album
        {
            song.Album = null;
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
                        : GetAlbumName(song.Artist, albName ?? Guid.NewGuid().ToString()); // Else we generate one
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
                    if (!Utils.SaveUrlAsImage(_client, source, GetImagePath(folder, key, "webp")))
                    {
                        return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
                        {
                            Success = false,
                            Reason = "Image URL is invalid"
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
                        if (!Utils.SaveUrlAsImage(_client, source, GetImagePath(folder, key, "webp")))
                        {
                            return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
                            {
                                Success = false,
                                Reason = "Image URL is invalid"
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

    private string DownloadSong(string youtube, string rawSongPath, string normSongPath)
    {
        int code; string err;
        Utils.ExecuteProcess(new("yt-dlp", $"{youtube} -o \"{rawSongPath}\" -x --audio-format {AudioFormat} -q --progress"), out code, out err);
        if (code != 0)
        {
            return $"yt-dlp {youtube} -o \"{rawSongPath}\" -x --audio-format {AudioFormat} -q --progress failed:\n{string.Join("", err.TakeLast(1000))}";
        }
        Utils.ExecuteProcess(new("ffmpeg-normalize", $"\"{rawSongPath}\" -pr -ext {AudioFormat} -o \"{normSongPath}\" -c:a libmp3lame"), out code, out err);
        if (code != 0)
        {
            return $"ffmpeg-normalize \"{rawSongPath}\" -pr -ext {AudioFormat} -o \"{normSongPath}\" -c:a libmp3lame failed:\n{string.Join("", err.TakeLast(1000))}";
        }
        return null;
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

        var err = DownloadSong(song.Source, rawPath, normPath);

        return StatusCode(err == null ? StatusCodes.Status200OK : StatusCodes.Status500InternalServerError, new BaseResponse()
        {
            Success = err == null,
            Reason = err
        });
    }

    [HttpPost("upload")]
    [Authorize]
    public IActionResult UploadSong([FromForm]YoutubeForm data)
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

        // Download from YouTube and normalize
        var err = DownloadSong(data.Youtube, rawSongPath, normSongPath);
        if (err != null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new BaseResponse()
            {
                Success = false,
                Reason = err
            });
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

    private string GetAlbumName(string? artist, string album)
        => $"{Utils.CleanPath(artist?.Trim() ?? "unknown")}_{Utils.CleanPath(album.Trim())}";

    private string GetSongName(string song, string? artist)
        => $"{Utils.CleanPath(song.Trim())}_{Utils.CleanPath(artist?.Trim() ?? "unknown")}";

    private string GetImagePath(string dataFolder, string albumName, string ext)
        => $"{dataFolder}icon/{albumName}.{ext}";

    private string GetRawMusicPath(string dataFolder, string musicKey)
        => $"{dataFolder}raw/{musicKey}";

    private string GetNormalizedMusicPath(string dataFolder, string musicKey)
        => $"{dataFolder}normalized/{musicKey}";
}
