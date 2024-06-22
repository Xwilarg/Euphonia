﻿using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Downloader.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
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

        if (Design.IsDesignMode)
        {
            // XAML preview doesn't need to do anything
            _data = new();
        }
        else if (!Directory.Exists("data"))
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
        if (!Design.IsDesignMode)
        {
            if (!Directory.Exists("data/icon"))
            {
                Directory.CreateDirectory("data/icon");
            }
            if (!Directory.Exists("data/raw"))
            {
                Directory.CreateDirectory("data/raw");
            }
            if (!Directory.Exists("data/normalized"))
            {
                Directory.CreateDirectory("data/normalized");
            }
        }

        PlaylistChoices = [
            "None",
            .. _data.Playlists.Select(x => x.Value.Name)
        ];

        ClearAll();

        DownloadCmd = ReactiveCommand.Create(() =>
        {
            IsDownloading = true;

            AlbumName = AlbumName.Trim();
            Artist = Artist.Trim();
            SongName = SongName.Trim();
            MusicUrl = MusicUrl.Trim();
            SongType = SongType.Trim();

            if (_data.Musics.Any(x => x.Name == SongName && x.Artist == Artist && x.Type == SongType))
            {
                MessageBoxManager.GetMessageBoxStandard("Song already downloaded", $"A song of the same name, same artist and same type was already downloaded", icon: Icon.Info);
                IsDownloading = false;
                return;
            }

            string? imagePath = CanInputAlbumUrl ? "tmpLogo.png" : null;
            var musicPath = $"tmpMusicRaw.{AudioFormat}";
            var normMusicPath = $"tmpMusicNorm.{AudioFormat}";

            // Just in case
            if (imagePath != null && File.Exists(imagePath)) File.Delete(imagePath);
            if (File.Exists(musicPath)) File.Delete(musicPath);
            if (File.Exists(normMusicPath)) File.Delete(normMusicPath);


            _ = Task.Run(async () =>
            {
                try
                {
                    // Download all
                    if (CanInputAlbumUrl)
                    {
                        using var ms = new MemoryStream();
                        await foreach (var prog in DownloadAndFollowAsync(AlbumUrl, ms, new()))
                        {
                            DownloadImage = prog;
                        }
                        ms.Position = 0;
                        var bmp = new Bitmap(ms);
                        bmp.Save(imagePath!);
                    }
                    else
                    {
                        DownloadImage = 1f;
                    }

                    await foreach (var prog in ExecuteAndFollowAsync(new("yt-dlp", $"{MusicUrl} -o {musicPath} -x --audio-format {AudioFormat} -q --progress"), (s) =>
                    {
                        var m = Regex.Match(s, "([0-9.]+)%");
                        if (!m.Success) return -1f;
                        return float.Parse(m.Groups[1].Value) / 100f;
                    }))
                    {
                        DownloadMusic = prog;
                    }

                    await foreach (var prog in ExecuteAndFollowAsync(new("ffmpeg-normalize", $"{musicPath} -pr -ext {AudioFormat} -o {normMusicPath} -c:a libmp3lame"), (_) =>
                    {
                        return 0f;
                    }))
                    {
                        NormalizeMusic = prog;
                    }

                    var outMusicPath = $"{CleanPath(SongName)}_{CleanPath(Artist)}";
                    if (!string.IsNullOrWhiteSpace(SongType))
                    {
                        outMusicPath += $"_{SongType}";
                    }
                    outMusicPath += $".{AudioFormat}";
                    var m = new Song
                    {
                        Album = string.IsNullOrWhiteSpace(AlbumName) ? null : AlbumName,
                        Artist = Artist,
                        Name = SongName,
                        Path = outMusicPath,
                        Playlist = PlaylistIndex == 0 ? "default" : _data.Playlists.Keys.ElementAt(PlaylistIndex - 1),
                        Source = MusicUrl,
                        Type = SongType
                    };

                    _data.Musics.Add(m);
                    if (imagePath != null)
                    {
                        File.Move(imagePath, $"data/icon/{CleanPath(AlbumName)}.png");
                    }
                    File.Move(musicPath, $"data/raw/{outMusicPath}");
                    File.Move(normMusicPath, $"data/normalized/{outMusicPath}");

                    ClearAll();
                }
                catch (Exception e)
                {
                    DownloadImage = 0f;
                    DownloadMusic = 0f;
                    NormalizeMusic = 0f;
                    MessageBoxManager.GetMessageBoxStandard("Download failed", $"An error occurred while downloading your music: {e.Message}", icon: Icon.Error);
                }
                finally
                {
                    IsDownloading = false;
                }
            });
        });
    }

    private async IAsyncEnumerable<float> ExecuteAndFollowAsync(ProcessStartInfo startInfo, Func<string, float> parseMethod)
    {
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.UseShellExecute = false;

        var p = Process.Start(startInfo);
        p.Start();

        var stdout = p.StandardOutput;
        //var stderr = p.StandardError;

        string line = stdout.ReadLine();
        while (line != null)
        {
            var r = parseMethod(line);
            if (r >= 0f)
            {
                yield return r;
            }
            line = stdout.ReadLine();
        }

        p.WaitForExit();
        yield return 1f;
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

    private string CleanPath(string name)
    {
        var forbidden = new[] { '<', '>', ':', '\\', '/', '"', '|', '?', '*' };
        foreach (var c in forbidden)
        {
            name = name.Replace(c.ToString(), string.Empty);
        }
        return name;
    }

    private bool CleanCompare(string a, string b)
    {
        return a.Trim().ToUpperInvariant() == b.Trim().ToUpperInvariant();
    }

    private void ClearAll()
    {
        SongName = string.Empty;
        Artist = string.Empty;
        MusicUrl = string.Empty;
        AlbumName = string.Empty;
        AlbumUrl = string.Empty;
        SongType = string.Empty;
        PlaylistIndex = 0;

        DownloadImage = 0f;
        DownloadMusic = 0f;
        NormalizeMusic = 0f;

        SongCount = $"{_data.Musics.Count} music found";
    }

    private JsonExportData _data;
    private JsonSerializerOptions _jsonOptions;
    private HttpClient _client;

    private const string AudioFormat = "mp3";

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

    public int _playlistIndex;
    /// <summary>
    /// What playlist this song belong to
    /// </summary>
    public int PlaylistIndex
    {
        get => _playlistIndex;
        set => this.RaiseAndSetIfChanged(ref _playlistIndex, value);
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

    private float _downloadMusic;
    /// <summary>
    /// Progress of the download of the song
    /// </summary>
    public float DownloadMusic
    {
        get => _downloadMusic;
        set => this.RaiseAndSetIfChanged(ref _downloadMusic, value);
    }

    private float _normalizeMusic;
    /// <summary>
    /// Progress of the normalization of the song
    /// </summary>
    public float NormalizeMusic
    {
        get => _normalizeMusic;
        set => this.RaiseAndSetIfChanged(ref _normalizeMusic, value);
    }
}
