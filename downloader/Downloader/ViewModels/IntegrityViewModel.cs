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
                    try
                    {
                        int i = 0;
                        foreach (var m in _mainViewModel.Data.Musics)
                        {
                            var musicKey = _mainViewModel.GetMusicKey(m.Name, m.Artist, m.Type);
                            var rawSongPath = _mainViewModel.GetRawMusicPath(musicKey);
                            var normSongPath = _mainViewModel.GetNormalizedMusicPath(musicKey);

                            if (File.Exists(normSongPath)) continue;

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

                            i++;
                            VerifyProgress = i / (float)_mainViewModel.Data.Musics.Count;
                        }
                    }
                    catch (Exception ex)
                    {
                        var mainWindow = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;
                        Dispatcher.UIThread.Post(async () =>
                        {
                            await MessageBoxManager.GetMessageBoxStandard("Error while verifying song", $"Error while verifying song: {ex.Message}", icon: Icon.Error).ShowAsPopupAsync(mainWindow);
                        });
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
