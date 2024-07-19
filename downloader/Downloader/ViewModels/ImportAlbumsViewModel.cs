using Downloader.Models;

namespace Downloader.ViewModels;

public class ImportAlbumsViewModel : ViewModelBase, ITabView
{
    public ImportAlbumsViewModel() : this(null)
    { }

    public ImportAlbumsViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        _mainViewModel?.Views?.Add(this);
    }

    public void AfterInit()
    { }

    private MainViewModel _mainViewModel;
}