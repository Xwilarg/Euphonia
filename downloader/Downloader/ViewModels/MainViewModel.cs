using Downloader.Models;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Downloader.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _client = new();

        if (!Directory.Exists("data"))
        {
            Directory.CreateDirectory("data");
            _data = new();
        }
        else
        {
            if (File.Exists("data/info.json"))
            {
                _data = JsonSerializer.Deserialize<JsonExportData>(File.ReadAllText("data/info.json"), _jsonOptions) ?? new();
            }
            else
            {
                _data = new();
            }
        }
        PlaylistChoices = [
            "None",
            .. _data.Playlists.Keys
        ];

        SongCount = $"{_data.Musics.Count} music found";

        DownloadCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            IsDownloading = true;
            try
            {
                var imagePath = CanInputAlbumUrl ? $"tmpLogo{Path.GetExtension(AlbumUrl)}" : null;
                if (imagePath != null)
                {
                    using var file = new FileStream(imagePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await foreach (var prog in DownloadAndFollowAsync(AlbumUrl, file, new()))
                    {
                        DownloadImage = prog;
                    }
                }
            }
            finally
            {
                IsDownloading = false;
            }
        });
    }

    private async IAsyncEnumerable<float> DownloadAndFollowAsync(string url, Stream destination, CancellationToken token)
    {
        using var response = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        var contentLength = response.Content.Headers.ContentLength;

        using var download = await response.Content.ReadAsStreamAsync(token);

        if (!contentLength.HasValue)
        {
            await download.CopyToAsync(destination);
            yield return 1f;
        }
        else
        {
            var buffer = new byte[8192];
            float totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await download.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, token).ConfigureAwait(false);
                totalBytesRead += bytesRead;
                yield return totalBytesRead / contentLength.Value;
            }

            yield return 1f;
        }
    }

    private bool CleanCompare(string a, string b)
    {
        return a.Trim().ToUpperInvariant() == b.Trim().ToUpperInvariant();
    }

    private JsonExportData _data;
    private JsonSerializerOptions _jsonOptions;
    private HttpClient _client;

    public ICommand DownloadCmd { get; }

    private string _songName;
    /// <summary>
    /// Name of the song
    /// </summary>
    public string SongName
    {
        get => _songName;
        set => this.RaiseAndSetIfChanged(ref _songName, value);
    }

    private string _artist;
    /// <summary>
    /// Name of the artist
    /// </summary>
    public string Artist
    {
        get => _artist;
        set => this.RaiseAndSetIfChanged(ref _artist, value);
    }

    private string _musicUrl;
    /// <summary>
    /// URL to the song
    /// </summary>
    public string MusicUrl
    {
        get => _musicUrl;
        set => this.RaiseAndSetIfChanged(ref _musicUrl, value);
    }

    private string _albumName;
    /// <summary>
    /// Name of the album
    /// </summary>
    public string AlbumName
    {
        get => _albumName;
        set
        {
            this.RaiseAndSetIfChanged(ref _albumName, value);
            if (string.IsNullOrWhiteSpace(value))
            {
                CanInputAlbumUrl = false;
            }
            else if (_data.Albums.Any(x => CleanCompare(x.Key, value)))
            {
                CanInputAlbumUrl = false;
            }
            else
            {
                CanInputAlbumUrl = true;
            }
        }
    }

    private string _albumUrl;
    /// <summary>
    /// URL to the album image
    /// </summary>
    public string AlbumUrl
    {
        get => _albumUrl;
        set => this.RaiseAndSetIfChanged(ref _albumUrl, value);
    }

    private string _songType;
    /// <summary>
    /// Type of the song
    /// </summary>
    public string SongType
    {
        get => _songType;
        set => this.RaiseAndSetIfChanged(ref _songType, value);
    }

    public string _playlist;
    /// <summary>
    /// What playlist this song belong to
    /// </summary>
    public string Playlist
    {
        get => _playlist;
        set => this.RaiseAndSetIfChanged(ref _playlist, value);
    }
    public string[] PlaylistChoices { private set; get; }

    private string _songCount;
    /// <summary>
    /// Number of songs already downloaded
    /// </summary>
    public string SongCount
    {
        get => _songCount;
        set => this.RaiseAndSetIfChanged(ref _songCount, value);
    }

    private bool _canInputAlbumUrl;
    /// <summary>
    /// Can we type the album URL
    /// e.g. did we not already download it for a previous song
    /// </summary>
    public bool CanInputAlbumUrl
    {
        get => _canInputAlbumUrl;
        set => this.RaiseAndSetIfChanged(ref _canInputAlbumUrl, value);
    }

    private bool _isDownloading;
    /// <summary>
    /// Are we currently downloading everything
    /// </summary>
    public bool IsDownloading
    {
        get => _isDownloading;
        set => this.RaiseAndSetIfChanged(ref _isDownloading, value);
    }

    private float _downloadImage;
    /// <summary>
    /// Progress of the download of the image
    /// </summary>
    public float DownloadImage
    {
        get => _downloadImage;
        set => this.RaiseAndSetIfChanged(ref _downloadImage, value);
    }
}
