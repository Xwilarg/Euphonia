using Euphonia.API.Models.Request;
using Euphonia.API.Models.Request.Upload;
using Euphonia.API.Models.Response;
using Euphonia.API.Services;
using Euphonia.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Euphonia.API.Controllers;

[ApiController]
[Route("/api/download/")]
public class DownloadController : ControllerBase
{

    private readonly ILogger<DownloadController> _logger;
    private DownloaderManager _download;
    private HttpClient _client;
    private ExportManager _export;

    public DownloadController(ILogger<DownloadController> logger, HttpClient client, DownloaderManager download, ExportManager export)
    {
        _logger = logger;
        _client = client;

        _download = download;
        _export = export;
    }


    [HttpPost("progress")]
    [Authorize]
    public IActionResult GetProgress()
    {
        var exportData = _export.GetExportPath();

        return StatusCode(StatusCodes.Status200OK, new SongDownloadResponse()
        {
            Success = true,
            Reason = null,
            Data = _download.GetProgress(),
            Export = (exportData == null || exportData.IsBusy == ExportStatus.None) ? null : new()
            {
                ExportPath = exportData.LastFile,
                Status = exportData.IsBusy
            }
        });
    }

    private Song? LookupSong(string key, out EuphoniaInfo info)
    {
        info = Serialization.Deserialize<EuphoniaInfo>(System.IO.File.ReadAllText($"/data/info.json"));

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
        var song = LookupSong(data.Key, out var info);

        if (song == null)
        {
            return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
            {
                Success = false,
                Reason = "Can't find a song with the given key"
            });
        }

        if (song.Source == null)
        {
            return StatusCode(StatusCodes.Status400BadRequest, new BaseResponse()
            {
                Success = false,
                Reason = "Song source isn't set"
            });
        }
        else if (song.Source == "localfile")
        {
            var rawPath = GetRawMusicPath(song.RawPath ?? song.Path);
            var normPath = GetNormalizedMusicPath(song.Path);
            System.IO.File.Delete(normPath);

            _download.QueueToNormalize(song, rawPath, normPath);
        }
        else
        {
            var rawPath = GetRawMusicPath(song.RawPath ?? song.Path);
            var normPath = GetNormalizedMusicPath(song.Path);
            System.IO.File.Delete(rawPath);
            System.IO.File.Delete(normPath);

            _download.QueueToDownload(song, song.Source, rawPath, normPath);
        }

        return StatusCode(StatusCodes.Status200OK, new BaseResponse()
        {
            Success = true,
            Reason = null
        });
    }

    private IActionResult? UploadSongInternal(AUploadForm data, out Song? song, out string? rawSongPath, out string? normSongPath, string extension)
    {
        string? thumbnailHash;
        // Download album image
        if (data.CoverUrl != null)
        {
            thumbnailHash = Utils.Sha256(data.CoverUrl);
            if (!Utils.SaveUrlAsImage(_client, data.CoverUrl, GetImagePath(thumbnailHash, "webp"), out var error))
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
        else thumbnailHash = null;

        // Prepare to download the rest
        var rawKey = GetMusicKey(data.Name, data.Artist, data.SongType, extension);
        var normKey = GetMusicKey(data.Name, data.Artist, data.SongType, AudioFormat);

        rawSongPath = GetRawMusicPath(rawKey);
        normSongPath = GetNormalizedMusicPath(normKey);
        var info = Serialization.Deserialize<EuphoniaInfo>(System.IO.File.ReadAllText($"/data/info.json"));


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

        info = Serialization.Deserialize<EuphoniaInfo>(System.IO.File.ReadAllText($"/data/info.json")); // Load again for concurency issues

        // We sanitize user inputs just in case
        var songName = data.Name.Trim();
        var artist = data.Artist?.Trim();
        var songType = data.SongType?.Trim();
        var album = data.AlbumName?.Trim();
        if (string.IsNullOrWhiteSpace(songType)) songType = null;

        // Create Song class
        song = new Song
        {
            Key = Guid.NewGuid().ToString(),
            Artist = artist,
            AlbumName = album,
            Name = songName,
            RawPath = rawKey,
            Path = normKey,
            Playlists = data.Playlists == null ? [] : data.Playlists,
            Source = data is YoutubeForm ytForm ? ytForm.Youtube : "localfile",
            Type = songType,
            ThumbnailHash = thumbnailHash
        };
        info.Musics.Add(song);

        // If album exists we add it to the JSON too
        if (thumbnailHash != null && !info.AlbumHashes.ContainsKey(thumbnailHash))
        {
            info.AlbumHashes.Add(thumbnailHash, $"{thumbnailHash}.webp");
        }

        System.IO.File.WriteAllText($"/data/info.json", Serialization.Serialize(info));

        return null;
    }

    [HttpPost("upload/youtube")]
    [Authorize]
    public IActionResult UploadSongYoutube([FromForm] YoutubeForm data)
    {
        var sc = UploadSongInternal(data, out Song? song, out string? rawSongPath, out string? normSongPath, AudioFormat);

        if (sc != null)
        {
            return sc;
        }

        _download.QueueToDownload(song!, data.Youtube, rawSongPath!, normSongPath!);
        return StatusCode(StatusCodes.Status200OK, new BaseResponse()
        {
            Success = true,
            Reason = null
        });
    }


    [HttpPost("upload/local")]
    [Authorize]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> UploadSongLocal([FromForm] LocalFileForm data)
    {
        var sc = UploadSongInternal(data, out Song? song, out string? rawSongPath, out string? normSongPath, Path.GetExtension(data.LocalFile.FileName));

        if (sc != null)
        {
            return sc;
        }

        // Save file to disk
        using Stream fileStream = new FileStream(rawSongPath!, FileMode.Create);
        await data.LocalFile.CopyToAsync(fileStream);

        // Queue normalization
        _download.QueueToNormalize(song!, rawSongPath!, normSongPath!);

        return StatusCode(StatusCodes.Status200OK, new BaseResponse()
        {
            Success = true,
            Reason = null
        });
    }

    public string AudioFormat => DownloaderManager.AudioFormat;

    private string GetMusicKey(string songName, string? artist, string? songType, string extension)
    {
        var outMusicPath = GetSongName(songName, artist);
        if (!string.IsNullOrWhiteSpace(songType))
        {
            outMusicPath += $"_{songType}";
        }
        outMusicPath += $".{extension}";
        return outMusicPath;
    }

    public static string GetSongName(string song, string? artist)
        => $"{Utils.CleanPath(song.Trim())}_{Utils.CleanPath(artist?.Trim() ?? "unknown")}";

    public static string GetImagePath(string albumName, string ext)
        => $"/data/icon/{albumName}.{ext}";

    public static string GetRawMusicPath(string musicKey)
        => $"/data/raw/{musicKey}";

    public static string GetNormalizedMusicPath(string musicKey)
        => $"/data/normalized/{musicKey}";
}
