using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Downloader.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using System;
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
                    {
                        string songName = string.Empty;
                        try
                        {
                            var filename = Path.GetFileNameWithoutExtension(g);
                            songName = GetRegexMatch(filename, RegexSongName ?? string.Empty, int.TryParse(GroupSongName, out var resGroupName) ? resGroupName : 0) ?? filename;
                            var artist = GetRegexMatch(filename, RegexSongArtist ?? string.Empty, int.TryParse(GroupSongArtist, out var resGroupArtist) ? resGroupArtist : 0);

                            var musicKey = _mainViewModel.GetMusicKey(songName, artist, null);
                            var rawPath = _mainViewModel.GetRawMusicPath(musicKey);
                            var normPath = _mainViewModel.GetNormalizedMusicPath(musicKey);

                            File.Copy(g, rawPath);
                            prog++;
                            ImportSong = prog / (_allFiles.Length * 3f);

                            await foreach (var _ in ProcessManager.Normalize(rawPath, normPath)) {}
                            prog++;
                            ImportSong = prog / (_allFiles.Length * 3f);

                            _mainViewModel.AddMusic(
                                songName,
                                "localfile",
                                artist,
                                null,
                                null,
                                null,
                                0
                                );
                            prog++;
                            ImportSong = prog / (_allFiles.Length * 3f);
                        }
                        catch (Exception ex)
                        {
                            var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
                            bool? shouldReturnTrue = null;
                            Dispatcher.UIThread.Post(async () =>
                            {
                                var answer = await MessageBoxManager.GetMessageBoxStandard("Error while adding music", $"Error while adding {songName}: {ex.Message}\nDo you still want to continue?", ButtonEnum.OkAbort, icon: Icon.Error).ShowAsPopupAsync(mainWindow);
                                shouldReturnTrue = answer == ButtonResult.Ok;
                            });
                            while (shouldReturnTrue == null) await Task.Delay(100);
                            if (!shouldReturnTrue.Value) return;
                        }
                    }
                }
                finally
                {
                    IsImporting = false;
                    ImportSong = 0;
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
        UpdatePreview();
    }

    public void OnDataRefresh()
    { }

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