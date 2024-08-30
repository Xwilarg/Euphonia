using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Downloader.Models;
using DynamicData;
using Euphonia.Common;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Downloader.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        Client = new();

        try
        {
            if (!Directory.Exists("Data/"))
            {
                Directory.CreateDirectory("Data/");
            }
            if (!File.Exists("Data/info.json"))
            {
                File.WriteAllText("Data/info.json", "{}");
            }
        }
        catch { }

        SelectDataPathCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
            var files = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Data",
                AllowMultiple = false,
                FileTypeFilter = [new("JSON file") { Patterns = ["*.json"], MimeTypes = ["application/json"] }]
            });
            if (files.Any())
            {
                var target = files[0];
                if (DataImportChoices.Contains(target.Path.LocalPath))
                {
                    await MessageBoxManager.GetMessageBoxStandard("Project already loaded", "A project with this path was already loaded", icon: Icon.Error).ShowAsPopupAsync(mainWindow);
                }
                else
                {
                    DataImportChoices = [
                        ..DataImportChoices,
                        target.Path.LocalPath
                    ];
                    DataImportIndex = DataImportChoices.Length - 1;
                }
            }
        });

        CreateNewJson = ReactiveCommand.CreateFromTask(async () =>
        {
            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;

            try
            {
                if (File.Exists("Data/info.json"))
                {
                    var answer = await MessageBoxManager.GetMessageBoxStandard("File already exists", "There is already an info file exists, continue?", ButtonEnum.YesNo, Icon.Warning).ShowAsPopupAsync(mainWindow);
                    if (answer == ButtonResult.No)
                    {
                        return;
                    }
                    Directory.Delete("Data/", true);
                    Directory.CreateDirectory("Data/");
                }
                else
                {
                    if (!Directory.Exists("Data/"))
                    {
                        Directory.CreateDirectory("Data/");
                    }
                }
                File.WriteAllText("Data/info.json", "{}");
                var target = new FileInfo("Data/info.json").FullName;
                if (!DataImportChoices.Contains(target))
                {
                    DataImportChoices = [..DataImportChoices, target];
                    DataImportIndex = DataImportChoices.Length - 1;
                }
                else
                {
                    DataImportIndex = DataImportChoices.IndexOf(target);
                }
            }
            catch (Exception e)
            {
                await MessageBoxManager.GetMessageBoxStandard("Error while creating data", e.Message, icon: Icon.Error).ShowAsPopupAsync(mainWindow);
                DataPath = null;
            }
        });
    }

    public void LateInit()
    {
        if (Design.IsDesignMode) return;

        string[] possiblePaths = ["../../../../../web/data/info.json", "Data/info.json"];
        List<string> validData = new();
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                validData.Add(new FileInfo(path).FullName);
            }
        }

        if (validData.Any())
        {
            DataImportChoices = [.. validData];
        }
        else
        {
            if (!Directory.Exists("Data/info.json")) Directory.CreateDirectory("Data/");
            File.WriteAllText("Data/info.json", "{}");
            DataImportChoices = [ new FileInfo("Data/info.json").FullName ];
        }
        DataImportIndex = 0;
    }

    public const string AudioFormat = "mp3";

    public void SaveData()
    {
        File.WriteAllText(DataPath, Serialization.Serialize(Data));
    }

    public void SaveImage(string key, string name, string source)
    {
        Data.Albums.Add(key, new()
        {
            Name = name,
            Path = $"{key}.png",
            Source = source
        });
    }

    public string GetImagePath(string albumName)
        => $"{DataFolderPath}/icon/{albumName}.png";

    public string GetRawMusicPath(string musicKey)
        => $"{DataFolderPath}/raw/{musicKey}";

    public string GetNormalizedMusicPath(string musicKey)
        => $"{DataFolderPath}/normalized/{musicKey}";

    public string GetMusicKey(string songName, string artist, string songType)
    {
        var outMusicPath = GetSongName(songName, artist);
        if (!string.IsNullOrWhiteSpace(songType))
        {
            outMusicPath += $"_{songType}";
        }
        outMusicPath += $".{AudioFormat}";
        return outMusicPath;
    }

    public void AddMusic(string songName, string? source, string? artist, string? album, string? albumUrl, string? songType, int playlistIndex)
    {
        // We sanitize user inputs just in case
        songName = songName.Trim();
        artist = artist?.Trim();
        songType = songType?.Trim();
        album = album?.Trim();
        albumUrl = albumUrl?.Trim();
        source = source?.Trim();
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
            Playlist = playlistIndex == 0 ? "default" : Data.Playlists.Keys.ElementAt(playlistIndex - 1),
            Source = source,
            Type = songType
        };
        Data.Musics.Add(m);

        // If album exists we add it to the JSON too
        if (hasAlbum && !Data.Albums.ContainsKey(album))
        {
            SaveImage(albumKey, albumKey, albumUrl);
        }

        // Update the JSON with all we did
        SaveData();
        UpdateMainUI();
        foreach (var view in Views) view.OnDataRefresh();
    }

    public string CleanPath(string name)
    {
        var forbidden = new[] { '<', '>', ':', '\\', '/', '"', '|', '?', '*', '#', '&', '%' };
        foreach (var c in forbidden)
        {
            name = name.Replace(c.ToString(), string.Empty);
        }
        return name;
    }

    public string GetAlbumName(string? artist, string album)
        => $"{CleanPath(artist?.Trim() ?? "unknown")}_{CleanPath(album.Trim())}";

    public string GetSongName(string song, string? artist)
        => $"{CleanPath(song.Trim())}_{CleanPath(artist?.Trim() ?? "unknown")}";

    private EuphoniaInfo _data;
    public EuphoniaInfo Data
    {
        private set
        {
            _data = value;
        }
        get => _data;
    }
    public HttpClient Client { private set; get; }
    public ICommand SelectDataPathCmd { get; }
    public ICommand CreateNewJson { get; }

    public void Init()
    {
        Data = Serialization.Deserialize<EuphoniaInfo>(File.ReadAllText(DataPath)) ?? throw new NullReferenceException();

        if (!Directory.Exists($"{DataFolderPath}/icon"))
        {
            Directory.CreateDirectory($"{DataFolderPath}/icon");
        }
        if (!Directory.Exists($"{DataFolderPath}/raw"))
        {
            Directory.CreateDirectory($"{DataFolderPath}/raw");
        }
        if (!Directory.Exists($"{DataFolderPath}/normalized"))
        {
            Directory.CreateDirectory($"{DataFolderPath}/normalized");
        }

        PlaylistChoices = [
            "None",
            .. Data.Playlists.Select(x => x.Value.Name)
        ];

        foreach (var v in Views)
        {
            v.AfterInit();
        }
    }

    public List<ITabView> Views { get; } = [];

    public void UpdateMainUI()
    {
        SongCount = $"{Data.Musics.Count} music found";
    }

    private string[] _playlistChoices;
    public string[] PlaylistChoices
    {
        get => _playlistChoices;
        set => this.RaiseAndSetIfChanged(ref _playlistChoices, value);
    }


    private string _songCount;
    /// <summary>
    /// Number of songs already downloaded
    /// </summary>
    public string SongCount
    {
        get => _songCount;
        set => this.RaiseAndSetIfChanged(ref _songCount, value);
    }

    public string DataFolderPath { private set; get; }
    private string _dataPath;
    public string DataPath
    {
        get => _dataPath;
        set
        {
            if (value == null) DataFolderPath = null;
            else DataFolderPath = new FileInfo(value).Directory.FullName;
            this.RaiseAndSetIfChanged(ref _dataPath, value);
        }
    }

    private string _title;
    /// <summary>
    /// Name of the song
    /// </summary>
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    private bool _anyPlaylistAvailable;
    public bool AnyPlaylistAvailable
    {
        get => _anyPlaylistAvailable;
        set => this.RaiseAndSetIfChanged(ref _anyPlaylistAvailable, value);
    }

    private int _dataImportIndex = -1;
    public int DataImportIndex
    {
        get => _dataImportIndex;
        set
        {
            this.RaiseAndSetIfChanged(ref _dataImportIndex, value);
            AnyPlaylistAvailable = value >= 0;
            if (AnyPlaylistAvailable)
            {
                DataPath = _dataImportChoices[value];
                _ = Task.Run(async () =>
                {
                    try
                    {
                        Init();
                    }
                    catch (Exception e)
                    {
                        var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
                        Dispatcher.UIThread.Post(async () =>
                        {
                            await MessageBoxManager.GetMessageBoxStandard("Project data can't be loaded", "Impossible to parse the selected project file, make sure it is a valid Euphonia file", icon: Icon.Error).ShowAsPopupAsync(mainWindow);
                        });
                        DataPath = null;
                    }
                });
            }
        }
    }

    private string[] _dataImportChoices;
    public string[] DataImportChoices
    {
        get => _dataImportChoices;
        set => this.RaiseAndSetIfChanged(ref _dataImportChoices, value);
    }
}
