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
            this.FindControl<YoutubeDownloadView>("YoutubeDownload").DataContext = new YoutubeDownloadViewModel(mainVM);
            this.FindControl<ImportSongsView>("ImportSongs").DataContext = new ImportSongsViewModel(mainVM);

            mainVM.LateInit();
        };
    }
}
