using Avalonia.Controls;
using Downloader.ViewModels;
using System;

namespace Downloader.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        DataContextChanged += (object _sender, EventArgs _args) =>
        {
            var mainVM = (MainViewModel)DataContext;
            var yt = this.FindControl<YoutubeDownloadView>("YoutubeDownload");
            yt.DataContext = new YoutubeDownloadViewModel(mainVM);

            mainVM.LateInit();
        };
    }
}
