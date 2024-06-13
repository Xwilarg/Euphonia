using Downloader.Models;
using ReactiveUI;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Downloader.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

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
    }

    private JsonExportData _data;
    private JsonSerializerOptions _jsonOptions;

    private string _songName;
    public string SongName
    {
        get => _songName;
        set => this.RaiseAndSetIfChanged(ref _songName, value);
    }

    private string _artist;
    public string Artist
    {
        get => _artist;
        set => this.RaiseAndSetIfChanged(ref _artist, value);
    }

    private string _musicUrl;
    public string MusicUrl
    {
        get => _musicUrl;
        set => this.RaiseAndSetIfChanged(ref _musicUrl, value);
    }

    private string _albumName;
    public string AlbumName
    {
        get => _albumName;
        set => this.RaiseAndSetIfChanged(ref _albumName, value);
    }

    private string _albumUrl;
    public string AlbumUrl
    {
        get => _albumUrl;
        set => this.RaiseAndSetIfChanged(ref _albumUrl, value);
    }

    private string _songType;
    public string SongType
    {
        get => _songType;
        set => this.RaiseAndSetIfChanged(ref _songType, value);
    }

    public string _playlist;
    public string Playlist
    {
        get => _playlist;
        set => this.RaiseAndSetIfChanged(ref _playlist, value);
    }
    public string[] PlaylistChoices { private set; get; }

    private string _songCount;
    public string SongCount
    {
        get => _songCount;
        set => this.RaiseAndSetIfChanged(ref _songCount, value);
    }
}
