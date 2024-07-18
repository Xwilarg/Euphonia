using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Downloader.Models;
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
    }

    public void LateInit()
    {
        var debugPath = "../../../../../web/data/info.json";
        if (File.Exists(debugPath))
        {
            DataPath = debugPath;
            Init();
        }
    }

    public JsonExportData Data { private set; get; }
    public JsonSerializerOptions JsonOptions { private set; get; }
    public HttpClient Client { private set; get; }
    public ICommand SelectDataPathCmd { get; }

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
    /// <summary>
    /// Name of the song
    /// </summary>
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
}
