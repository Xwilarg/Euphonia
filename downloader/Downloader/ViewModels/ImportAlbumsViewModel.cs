using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Downloader.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using System.Xml.Linq;

namespace Downloader.ViewModels;

public class ImportAlbumsViewModel : ViewModelBase, ITabView
{
    public ImportAlbumsViewModel() : this(null)
    { }

    public ImportAlbumsViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        _mainViewModel?.Views?.Add(this);

        ImportAll = ReactiveCommand.Create(() =>
        {
            IsImporting = true;
            _ = Task.Run(async () =>
            {
                try
                {
                    var songs = _mainViewModel.Data.Musics.Where(x => x.Album == null);
                    if (ImportLocalOnly)
                    {
                        songs = songs.Where(x => x.Source == "localfile");
                    }
                    foreach (var s in songs)
                    {
                        var reqUrl = $"https://ws.audioscrobbler.com/2.0/?method=track.getInfo&api_key={LastFmApiKey}&artist={HttpUtility.UrlEncode(s.Artist)}&track={HttpUtility.UrlEncode(s.Name)}&format=json";
                        var json = JsonSerializer.Deserialize<LastFmApi>(await _mainViewModel.Client.GetStringAsync(reqUrl));

                        if (json.Message != null && json.Track.Album != null && json.Track.Album.Image.Any())
                        {
                            string album = json.Track.Album.Title;
                            var url = json.Track.Album.Image.Last().Text;
                        }
                    }
                }
                catch (Exception ex)
                {
                    var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
                    Dispatcher.UIThread.Post(async () =>
                    {
                        await MessageBoxManager.GetMessageBoxStandard("Error while importing album", $"Error while importing album: {ex.Message}", icon: Icon.Error).ShowAsPopupAsync(mainWindow);
                    });
                }
                finally
                {
                    IsImporting = false;
                }
            });
        });
    }

    private void UpdateAffectedFiles()
    {
        var songs = _mainViewModel.Data.Musics.Where(x => x.Album == null);
        AffectedFiles = $"{(ImportLocalOnly ? songs.Count(x => x.Source == "localfile") : songs.Count())} songs are missing an album";
    }

    public void AfterInit()
    {
        UpdateAffectedFiles();
    }

    private MainViewModel _mainViewModel;

    public ICommand ImportAll { get; }

    private string _affectedFiles;
    public string AffectedFiles
    {
        get => _affectedFiles;
        set => this.RaiseAndSetIfChanged(ref _affectedFiles, value);
    }

    private bool _importLocalOnly;
    public bool ImportLocalOnly
    {
        get => _importLocalOnly;
        set
        {
            this.RaiseAndSetIfChanged(ref _importLocalOnly, value);
            UpdateAffectedFiles();
        }
    }

    private float _importAlbums;
    public float ImportAlbums
    {
        get => _importAlbums;
        set => this.RaiseAndSetIfChanged(ref _importAlbums, value);
    }

    private bool _isImporting;
    public bool IsImporting
    {
        get => _isImporting;
        set => this.RaiseAndSetIfChanged(ref _isImporting, value);
    }

    private string _lastFmApiKey;
    public string LastFmApiKey
    {
        get => _lastFmApiKey;
        set => this.RaiseAndSetIfChanged(ref _lastFmApiKey, value);
    }
}