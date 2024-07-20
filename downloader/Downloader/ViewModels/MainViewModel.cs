using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Downloader.Models;
using DynamicData;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Downloader.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

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
        File.WriteAllText(DataPath, JsonSerializer.Serialize(Data, JsonOptions));
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

    public async ValueTask<bool> AddMusicAsync(string songName, string? source, string? artist, string? album, string? albumUrl, string? songType, int playlistIndex, string? imagePath, string musicPath, string? normMusicPath, bool copyOnly)
    {
        try
        {
            // Create output path
            var outMusicPath = GetSongName(songName, artist);
            if (!string.IsNullOrWhiteSpace(songType))
            {
                outMusicPath += $"_{songType}";
            }
            else
            {
                songType = null;
            }

            // If file already exists, we do filename_2 (and increment that number if still exists)
            var incrementOutMusicPath = outMusicPath;
            int i = 2;
            while (File.Exists($"{DataFolderPath}/raw/{incrementOutMusicPath}.{AudioFormat}"))
            {
                incrementOutMusicPath = $"{outMusicPath}_{i}";
                i++;
            }
            outMusicPath = incrementOutMusicPath;

            // Add file extension
            outMusicPath += $".{AudioFormat}";

            // Format album data
            string albumKey = null;
            var hasAlbum = !string.IsNullOrWhiteSpace(album);
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

            // Copy (or move) the files
            Action<string, string> fileMethod = copyOnly ? File.Copy : File.Move;
            if (imagePath != null)
            {
                fileMethod(imagePath, $"{DataFolderPath}/icon/{albumKey}.png");
            }
            fileMethod(musicPath, $"{DataFolderPath}/raw/{outMusicPath}");
            if (normMusicPath == null)
            {
                normMusicPath = $"tmpMusicNorm.{AudioFormat}";
                await foreach (var prog in ProcessManager.Normalize(musicPath, normMusicPath)) { }
            }
            fileMethod(normMusicPath, $"{DataFolderPath}/normalized/{outMusicPath}");

            // Update the JSON with all we did
            SaveData();
            UpdateMainUI();
            foreach (var view in Views) view.OnDataRefresh();
            return true;
        }
        catch (Exception ex)
        {
            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
            Dispatcher.UIThread.Post(async () =>
            {
                await MessageBoxManager.GetMessageBoxStandard("Error while adding music", $"Error while adding {songName}: {ex.Message}", icon: Icon.Error).ShowAsPopupAsync(mainWindow);
            });
            return false;
        }
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
        => $"{CleanPath(artist ?? "unknown")}_{CleanPath(album)}";

    public string GetSongName(string song, string? artist)
        => $"{CleanPath(song)}_{CleanPath(artist ?? "unknown")}";

    private JsonExportData _data;
    public JsonExportData Data
    {
        private set
        {
            _data = value;
            Title = _data?.Metadata?.Title ?? "Unknown";
        }
        get => _data;
    }
    public JsonSerializerOptions JsonOptions { private set; get; }
    public HttpClient Client { private set; get; }
    public ICommand SelectDataPathCmd { get; }
    public ICommand CreateNewJson { get; }

    public void Init()
    {
        Data = JsonSerializer.Deserialize<JsonExportData>(File.ReadAllText(DataPath), JsonOptions) ?? throw new NullReferenceException();

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
