using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Downloader.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Downloader.ViewModels;

public class YoutubeDownloadViewModel : ViewModelBase, ITabView
{
    public YoutubeDownloadViewModel() : this(null)
    { }

    public YoutubeDownloadViewModel(MainViewModel mainViewModel)
    {
        MainViewModel = mainViewModel;
        MainViewModel?.Views?.Add(this);

        DownloadCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            IsDownloading = true;

            AlbumName = AlbumName.Trim();
            Artist = Artist.Trim();
            SongName = SongName.Trim();
            MusicUrl = MusicUrl.Trim();
            SongType = SongType.Trim();

            if (MainViewModel.Data.Musics.Any(x => x.Name == SongName && x.Artist == Artist && x.Type == SongType))
            {
                var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
                await MessageBoxManager.GetMessageBoxStandard("Song already downloaded", $"A song of the same name, same artist and same type was already downloaded", icon: Icon.Error).ShowAsPopupAsync(mainWindow);
                IsDownloading = false;
                return;
            }


            _ = Task.Run(async () =>
            {
                try
                {
                    var albumName = MainViewModel.GetAlbumName(Artist, AlbumName);
                    var imagePath = MainViewModel.GetImagePath(albumName);

                    if (CanInputAlbumUrl)
                    {
                        await foreach (var prog in ProcessManager.DownloadImageAsync(MainViewModel.Client, AlbumUrl, imagePath))
                        {
                            DownloadImage = prog;
                        }
                    }
                    else
                    {
                        DownloadImage = 1f;
                    }

                    var startTime = int.TryParse(StartTime, out int resStartTime) ? resStartTime : 0;
                    var endTime = int.TryParse(EndTime, out int resEndTime) ? resEndTime : 0;

                    bool needCut = startTime > 0 || endTime > 0;
                    if (needCut && endTime != 0 && endTime <= startTime)
                    {
                        throw new Exception("EndTime must either be 0 or superior at StartTime");
                    }

                    var musicKey = MainViewModel.GetMusicKey(SongName, Artist, SongType);
                    var rawSongPath = MainViewModel.GetRawMusicPath(musicKey);
                    var normSongPath = MainViewModel.GetRawMusicPath(musicKey);

                    if (File.Exists(normSongPath) || File.Exists(rawSongPath))
                    {
                        throw new Exception($"There is already a music saved with the same filename");
                    }

                    var keyCut = $"youtube_tmp.{MainViewModel.AudioFormat}";
                    var targetRawDownload = needCut ? keyCut : rawSongPath;
                    if (needCut)
                    {
                        File.Delete(keyCut);
                    }

                    await foreach (var prog in ProcessManager.ExecuteAndFollowAsync(new("yt-dlp", $"{MusicUrl} -o \"{targetRawDownload}\" -x --audio-format {MainViewModel.AudioFormat} -q --progress"), (s) =>
                    {
                        var m = Regex.Match(s, "([0-9.]+)%");
                        if (!m.Success) return -1f;
                        return float.Parse(m.Groups[1].Value) / 100f;
                    }))
                    {
                        DownloadMusic = prog;
                    }

                    if (needCut)
                    {
                        var duration = endTime == 0 ? 0 : endTime - startTime;
                        var durationArg = duration == 0 ? string.Empty : $" -t {duration} ";
                        await foreach (var prog in ProcessManager.ExecuteAndFollowAsync(new("ffmpeg", $"-ss {startTime} {durationArg} -i {targetRawDownload} \"{rawSongPath}\""), (s) =>
                        {
                            return 0f;
                        }))
                        {
                            CutMusic = prog;
                        }
                        File.Delete(keyCut);
                    }
                    else
                    {
                        CutMusic = 1f;
                    }

                    await foreach (var prog in ProcessManager.Normalize(rawSongPath, normSongPath))
                    {
                        NormalizeMusic = prog;
                    }

                    MainViewModel.AddMusic(
                        SongName,
                        MusicUrl,
                        Artist,
                        AlbumName,
                        AlbumUrl,
                        SongType,
                        PlaylistIndex);
                    ClearAll();
                }
                catch (Exception e)
                {
                    DownloadImage = 0f;
                    DownloadMusic = 0f;
                    CutMusic = 0f;
                    NormalizeMusic = 0f;
                    var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
                    Dispatcher.UIThread.Post(async () =>
                    {
                        await MessageBoxManager.GetMessageBoxStandard("Download failed", $"An error occurred while downloading your music: {e.Message}", icon: Icon.Error).ShowAsPopupAsync(mainWindow);
                    });
                }
                finally
                {
                    IsDownloading = false;
                }
            });
        });
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
        StartTime = string.Empty;
        EndTime = string.Empty;

        DownloadImage = 0f;
        DownloadMusic = 0f;
        CutMusic = 0f;
        NormalizeMusic = 0f;

        MainViewModel.UpdateMainUI();
    }

    public void AfterInit()
    {
        // Change to 0 doesn't update field because it's already the previous state
        PlaylistIndex = 1;
        PlaylistIndex = 0;

        ClearAll();
    }

    public void OnDataRefresh()
    { }

    public ICommand DownloadCmd { get; }

    private MainViewModel _mainViewModel;
    public MainViewModel MainViewModel
    {
        get => _mainViewModel;
        set => this.RaiseAndSetIfChanged(ref _mainViewModel, value);
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
            else if (MainViewModel.Data.Albums.Any(x => CleanCompare(MainViewModel.GetAlbumName(Artist, AlbumName), value)))
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

    private float _cutMusic;
    /// <summary>
    /// Cutting audio with ffmpeg
    /// </summary>
    public float CutMusic
    {
        get => _cutMusic;
        set => this.RaiseAndSetIfChanged(ref _cutMusic, value);
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

    private string _startTime = "0";
    public string StartTime
    {
        get => _startTime;
        set
        {
            if (string.IsNullOrEmpty(value)) value = "0";
            this.RaiseAndSetIfChanged(ref _startTime, value);
        }
    }

    private string _endTime = "0";
    public string EndTime
    {
        get => _endTime;
        set
        {
            if (string.IsNullOrEmpty(value)) value = "0";
            this.RaiseAndSetIfChanged(ref _endTime, value);
        }
    }
}
