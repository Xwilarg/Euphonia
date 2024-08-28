using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Downloader.Models;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ReactiveUI;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Downloader.ViewModels
{
    internal class IntegrityViewModel : ViewModelBase, ITabView
    {
        public IntegrityViewModel() : this(null)
        { }

        public IntegrityViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _mainViewModel?.Views?.Add(this);

            VerifyCmd = ReactiveCommand.CreateFromTask(async () =>
            {
                _ = Task.Run(async () =>
                {
                    IsVerifying = false;
                    try
                    {
                        int i = 0;
                        foreach (var m in _mainViewModel.Data.Musics)
                        {
                            try
                            {
                                var rawSongPath = $"{_mainViewModel.DataFolderPath}/raw/{m.Path}";
                                var normSongPath = $"{_mainViewModel.DataFolderPath}/normalized/{m.Path}";


                                if (m.Album != null)
                                {
                                    if (!_mainViewModel.Data.Albums.ContainsKey(m.Album))
                                    {
                                        // TODO
                                    }
                                    else if (!File.Exists(_mainViewModel.Data.Albums[m.Album].Path) && _mainViewModel.Data.Albums[m.Album].Source != null &&
                                        (_mainViewModel.Data.Albums[m.Album].Source.StartsWith("http://") || _mainViewModel.Data.Albums[m.Album].Source.StartsWith("https://")))
                                    {
                                        await foreach (var prog in ProcessManager.DownloadImageAsync(_mainViewModel.Client, _mainViewModel.Data.Albums[m.Album].Source, $"{_mainViewModel.DataFolderPath}/icon/{_mainViewModel.Data.Albums[m.Album].Path}")) { }
                                    }
                                }

                                if (!File.Exists(normSongPath))
                                {
                                    if (!File.Exists(rawSongPath))
                                    {
                                        await foreach (var prog in ProcessManager.Normalize(rawSongPath, normSongPath))
                                        { }
                                    }

                                    if (DownloadMissingSongs && (m.Source.StartsWith("https://youtu.be/") || m.Source.StartsWith("https://youtube.com/") || m.Source.StartsWith("https://www.youtube.com/")))
                                    {

                                        await foreach (var prog in ProcessManager.YouTubeDownload(m.Source, rawSongPath))
                                        { }
                                        await foreach (var prog in ProcessManager.Normalize(rawSongPath, normSongPath))
                                        { }
                                    }
                                }

                                i++;
                                VerifyProgress = i / (float)_mainViewModel.Data.Musics.Count;
                            }
                            catch (Exception ex)
                            {
                                var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
                                bool? shouldReturnTrue = null;
                                Dispatcher.UIThread.Post(async () =>
                                {
                                    var answer = await MessageBoxManager.GetMessageBoxStandard("Error while verifying song", $"Error while verifying {m.Name} by {m.Artist}: {ex.Message}\nDo you still want to continue?", ButtonEnum.OkAbort, icon: Icon.Error).ShowAsPopupAsync(mainWindow);
                                    shouldReturnTrue = answer == ButtonResult.Ok;
                                });
                                while (shouldReturnTrue == null) await Task.Delay(100);
                                if (!shouldReturnTrue.Value) return;
                            }
                        }
                    }
                    finally
                    {
                        VerifyProgress = 0f;
                        IsVerifying = false;
                    }
                });
            });
        }

        private MainViewModel _mainViewModel;

        public void AfterInit()
        { }

        public void OnDataRefresh()
        { }

        public ICommand VerifyCmd { get; }

        private bool _downloadMissingSongs;
        public bool DownloadMissingSongs
        {
            get => _downloadMissingSongs;
            set => this.RaiseAndSetIfChanged(ref _downloadMissingSongs, value);
        }

        private bool _isVerifying;
        public bool IsVerifying
        {
            get => _isVerifying;
            set => this.RaiseAndSetIfChanged(ref _isVerifying, value);
        }

        private float _verifyProgress;
        public float VerifyProgress
        {
            get => _verifyProgress;
            set => this.RaiseAndSetIfChanged(ref _verifyProgress, value);
        }
    }
}
