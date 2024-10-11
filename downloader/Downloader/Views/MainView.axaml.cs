using Avalonia.Controls;
using Downloader.ViewModels;
using System;

namespace Downloader.Views;

public partial class MainView : UserControl
{
    private bool _isInit;
    public MainView()
    {
        InitializeComponent();

        DataContextChanged += (object _sender, EventArgs _args) =>
        {
            if (_isInit) return;
            _isInit = true;
            var mainVM = (MainViewModel)DataContext;
            this.FindControl<ImportSongsView>("ImportSongs").DataContext = new ImportSongsViewModel(mainVM);
            this.FindControl<ImportAlbumsView>("ImportAlbums").DataContext = new ImportAlbumsViewModel(mainVM);
            this.FindControl<IntegrityView>("Integrity").DataContext = new IntegrityViewModel(mainVM);

            mainVM.LateInit();
        };
    }
}
