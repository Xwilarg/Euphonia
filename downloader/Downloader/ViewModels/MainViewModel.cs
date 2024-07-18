using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
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
using System.Text.Encodings.Web;
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
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        _client = new();

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
                var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
                MessageBoxManager.GetMessageBoxStandard("Song already downloaded", $"A song of the same name, same artist and same type was already downloaded", icon: Icon.Error).ShowAsPopupAsync(mainWindow);
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
                    var hasAlbum = !string.IsNullOrWhiteSpace(AlbumName);
                    var m = new Song
                    {
                        Album = hasAlbum ? AlbumName : null,
                        Artist = Artist,
                        Name = SongName,
                        Path = outMusicPath,
                        Playlist = PlaylistIndex == 0 ? "default" : _data.Playlists.Keys.ElementAt(PlaylistIndex - 1),
                        Source = MusicUrl,
                        Type = string.IsNullOrWhiteSpace(SongType) ? null : SongType
                    };

                    _data.Musics.Add(m);
                    var albumPath = $"{CleanPath(Artist)}_{CleanPath(AlbumName)}";
                    if (hasAlbum && !_data.Albums.ContainsKey(AlbumName))
                    {
                        _data.Albums.Add(albumPath, new()
                        {
                            Path = $"{albumPath}.png",
                        });
                    }
                    if (imagePath != null)
                    {
                        File.Move(imagePath, $"{_dataFolderPath}/icon/{CleanPath(AlbumName)}.png");
                    }
                    File.Move(musicPath, $"{_dataFolderPath}/raw/{outMusicPath}");
                    File.Move(normMusicPath, $"{_dataFolderPath}/normalized/{outMusicPath}");
                    File.WriteAllText(_dataPath, JsonSerializer.Serialize(_data, _jsonOptions));

                    ClearAll();
                }
                catch (Exception e)
                {
                    DownloadImage = 0f;
                    DownloadMusic = 0f;
                    NormalizeMusic = 0f;
                    var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
                    MessageBoxManager.GetMessageBoxStandard("Download failed", $"An error occurred while downloading your music: {e.Message}", icon: Icon.Error).ShowAsPopupAsync(mainWindow);
                }
                finally
                {
                    IsDownloading = false;
                }
            });
        });

        SelectDataPathCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
            var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Data",
                AllowMultiple = false,
                FileTypeFilter = [ new("JSON file") { Patterns = ["*.json"], MimeTypes = ["application/json"] }]
            });
            if (files.Any())
            {
                var target = files[0];
                DataPath = target.Path.LocalPath;
                try
                {
                    Init();
                    MessageBoxManager.GetMessageBoxStandard("Load succeed", $"The file was successfully loaded with {SongCount} songs", icon: Icon.Success).ShowAsPopupAsync(mainWindow);
                }
                catch
                {
                    MessageBoxManager.GetMessageBoxStandard("Project data can't be loaded", "Impossible to parse the selected project file, make sure it is a valid Euphonia file", icon: Icon.Error).ShowAsPopupAsync(mainWindow);
                    DataPath = null;
                }
            }
        });

        var debugPath = "../../../../../web/data/info.json";
        if (File.Exists(debugPath))
        {
            DataPath = debugPath;
            Init();
        }
    }

    public void Init()
    {
        _data = JsonSerializer.Deserialize<JsonExportData>(File.ReadAllText(DataPath), _jsonOptions) ?? throw new NullReferenceException();

        if (!Directory.Exists($"{_dataFolderPath}/icon"))
        {
            Directory.CreateDirectory($"{_dataFolderPath}/icon");
        }
        if (!Directory.Exists($"{_dataFolderPath}/raw"))
        {
            Directory.CreateDirectory($"{_dataFolderPath}/raw");
        }
        if (!Directory.Exists($"{_dataFolderPath}/normalized"))
        {
            Directory.CreateDirectory($"{_dataFolderPath}/normalized");
        }

        PlaylistChoices = [
            "None",
            .. _data.Playlists.Select(x => x.Value.Name)
        ];

        PlaylistIndex = 0;
        ClearAll();
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
        var forbidden = new[] { '<', '>', ':', '\\', '/', '"', '|', '?', '*', '#', '&', '%' };
        foreach (var c in forbidden)
        {
            name = name.Replace(c.ToString(), string.Empty);
        }
        return name;
    }

    private bool CleanCompare(string? a, string? b)
    {
        return a?.Trim()?.ToUpperInvariant() == b?.Trim()?.ToUpperInvariant();
    }

    private void ClearAll()
    {
        SongName = string.Empty;
        Artist = string.Empty;
        MusicUrl = string.Empty;
        AlbumName = string.Empty;
        AlbumUrl = string.Empty;
        SongType = string.Empty;

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
    public ICommand SelectDataPathCmd { get; }

    private string _dataFolderPath;
    private string _dataPath;
    /// <summary>
    /// Name of the song
    /// </summary>
    public string DataPath
    {
        get => _dataPath;
        set
        {
            if (value == null) _dataFolderPath = null;
            else _dataFolderPath = new FileInfo(value).Directory.FullName;
            this.RaiseAndSetIfChanged(ref _dataPath, value);
        }
    }

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
            else if (_data.Albums.Any(x => CleanCompare($"{CleanPath(Artist)}_{CleanPath(AlbumName)}", value)))
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
