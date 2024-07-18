using Downloader.Models;

namespace Downloader.ViewModels;

public class ImportSongsViewModel : ViewModelBase, ITabView
{
    public ImportSongsViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        _mainViewModel.Views.Add(this);
    }

    private MainViewModel _mainViewModel;

    public void AfterInit()
    { }
}
