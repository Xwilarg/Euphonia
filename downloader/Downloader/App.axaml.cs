using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Downloader.ViewModels;
using Downloader.Views;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System.Diagnostics;
using System.Linq;

namespace Downloader;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private bool DidExecutionSucceeed(string process, params string[] parameters)
    {
        var p = Process.Start(new ProcessStartInfo(process, string.Join(" ", parameters))
        {
            CreateNoWindow = true
        });
        p.WaitForExit();
        return p.ExitCode == 0;
    }

    private bool CheckIntegrity()
    {
        string[][] processes =
        [
            [ "ffmpeg", "-version" ],
            [ "ffmpeg-normalize", "--version" ],
            [ "yt-dlp", "--version"]
        ];
        foreach (var p in processes)
        {
            if (!DidExecutionSucceeed(p[0], p.Skip(1).ToArray()))
            {
                var dialog = MessageBoxManager
                    .GetMessageBoxStandard("Executable not found", $"Please install {p[0]} and make sure it is in your path",
                        ButtonEnum.Ok, Icon.Error);
                dialog.ShowAsync();
                return false;
            }
        }
        return true;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (!CheckIntegrity())
        {
            return;
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
