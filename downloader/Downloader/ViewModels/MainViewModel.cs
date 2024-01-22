using ReactiveUI;
using System.Windows.Input;

namespace Downloader.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        DownloadCmd = ReactiveCommand.Create(() =>
        {
        });
    }

    public string SongName
    {
        get => _songName;
        set => this.RaiseAndSetIfChanged(ref _songName, value);
    }
    public string YoutubeUrl
    {
        get => _youtubeUrl;
        set => this.RaiseAndSetIfChanged(ref _youtubeUrl, value);
    }
    public string Artist
    {
        get => _artist;
        set => this.RaiseAndSetIfChanged(ref _artist, value);
    }
    public string AlbumName
    {
        get => _albumName;
        set => this.RaiseAndSetIfChanged(ref _albumName, value);
    }
    public string AlbumUrl
    {
        get => _albumUrl;
        set => this.RaiseAndSetIfChanged(ref _albumUrl, value);
    }
    public string SongType
    {
        get => _songType;
        set => this.RaiseAndSetIfChanged(ref _songType, value);
    }


    private string _songName = string.Empty;
    private string _youtubeUrl = string.Empty;
    private string _artist = string.Empty;
    private string _albumName = string.Empty;
    private string _albumUrl = string.Empty;
    private string _songType = string.Empty;

    private ICommand DownloadCmd { get; }
}
