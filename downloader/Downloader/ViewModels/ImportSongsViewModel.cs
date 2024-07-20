using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Downloader.Models;
using ReactiveUI;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Downloader.ViewModels;

public class ImportSongsViewModel : ViewModelBase, ITabView
{
    public ImportSongsViewModel() : this(null)
    { }

    public ImportSongsViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        _mainViewModel?.Views?.Add(this);
        SearchFolder = ReactiveCommand.CreateFromTask(async () =>
        {
            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
            var folders = await mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Open Data",
                AllowMultiple = false
            });
            if (folders.Any())
            {
                DirName = folders[0].Path.LocalPath;
                _allFiles = Directory.GetFiles(DirName);
                ToImportFound = $"{_allFiles.Length} songs found";

                if (_allFiles.Any())
                {
                    _songPreview = _allFiles.Take(5).Select(x => Path.GetFileNameWithoutExtension(x)).ToArray();
                }
                else
                {
                    DirName = null;
                }
            }
            else
            {
                _songPreview = [];
            }

            ImportSong = 0;
            UpdatePreview();
        });

        ImportAll = ReactiveCommand.CreateFromTask(async () =>
        {
            IsImporting = true;
            _ = Task.Run(async() =>
            {
                try
                {
                    ImportSong = 0;
                    int prog = 0;
                    foreach (var g in _allFiles)
                    { // TODO: Revert or smth if this fails
                        var filename = Path.GetFileNameWithoutExtension(g);
                        if (!await _mainViewModel.AddMusicAsync(
                            GetRegexMatch(filename, RegexSongName ?? string.Empty, int.TryParse(GroupSongName, out var resGroupName) ? resGroupName : 0) ?? filename,
                            "localfile",
                            GetRegexMatch(filename, RegexSongArtist ?? string.Empty, int.TryParse(GroupSongArtist, out var resGroupArtist) ? resGroupArtist : 0),
                            null,
                            null,
                            null,
                            0,
                            null,
                            g,
                            null,
                            true
                            ))
                        {
                            ImportSong = 0;
                            break;
                        }
                        prog++;
                        ImportSong = (float)prog / _allFiles.Length;
                    }
                }
                finally
                {
                    IsImporting = false;
                }
            });
        });
    }

    private string GetRegexMatch(string input, string regex, int group)
    {
        if (!string.IsNullOrWhiteSpace(regex))
        {
            try
            {
                var matchName = Regex.Match(input, regex);
                if (matchName.Success && matchName.Groups.Count > group)
                {
                    return matchName.Groups[group].Value;
                }
            }
            catch
            { }
        }
        return null;
    }

    private void UpdatePreview()
    {
        var groupName = int.TryParse(GroupSongName, out var resGroupName) ? resGroupName : 0;
        var groupArtist = int.TryParse(GroupSongArtist, out var resGroupArtist) ? resGroupArtist : 0;
        var regexName = RegexSongName ?? string.Empty;
        var regexArtist = RegexSongArtist ?? string.Empty;

        StringBuilder str = new();
        foreach (var line in _songPreview)
        {
            string resultName = GetRegexMatch(line, regexName, groupName) ?? line;
            string resultArtist = GetRegexMatch(line, regexArtist, groupArtist) ?? string.Empty;

            str.AppendLine($"{resultName} by {resultArtist}");
        }

        PreviewRegexArea = str.ToString();
    }

    private MainViewModel _mainViewModel;
    public ICommand SearchFolder { get; }
    public ICommand ImportAll { get; }

    public void AfterInit()
    {
        ImportSong = 0;
        UpdatePreview();
    }

    private string[] _allFiles = [];
    private string[] _songPreview = [];

    private string _dirName;
    public string DirName
    {
        get => _dirName;
        set => this.RaiseAndSetIfChanged(ref _dirName, value);
    }

    private string _toImportFound = "Import a folder to start";
    public string ToImportFound
    {
        get => _toImportFound;
        set => this.RaiseAndSetIfChanged(ref _toImportFound, value);
    }

    private string _regexSongName;
    public string RegexSongName
    {
        get => _regexSongName;
        set
        {
            this.RaiseAndSetIfChanged(ref _regexSongName, value);
            UpdatePreview();
        }
    }

    private string _groupSongName;
    public string GroupSongName
    {
        get => _groupSongName;
        set
        {
            this.RaiseAndSetIfChanged(ref _groupSongName, value);
            UpdatePreview();
        }
    }

    private string _regexSongArtist;
    public string RegexSongArtist
    {
        get => _regexSongArtist;
        set
        {
            this.RaiseAndSetIfChanged(ref _regexSongArtist, value);
            UpdatePreview();
        }
    }

    private string _groupSongArtist;
    public string GroupSongArtist
    {
        get => _groupSongArtist;
        set
        {
            this.RaiseAndSetIfChanged(ref _groupSongArtist, value);
            UpdatePreview();
        }
    }

    private string _previewRegexArea;
    public string PreviewRegexArea
    {
        get => _previewRegexArea;
        set => this.RaiseAndSetIfChanged(ref _previewRegexArea, value);
    }

    private float _importSong;
    public float ImportSong
    {
        get => _importSong;
        set => this.RaiseAndSetIfChanged(ref _importSong, value);
    }

    private bool _isImporting;
    public bool IsImporting
    {
        get => _isImporting;
        set => this.RaiseAndSetIfChanged(ref _isImporting, value);
    }
}