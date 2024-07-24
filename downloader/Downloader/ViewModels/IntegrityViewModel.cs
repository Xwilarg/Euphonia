using Downloader.Models;

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
        }

        private MainViewModel _mainViewModel;

        public void AfterInit()
        { }

        public void OnDataRefresh()
        { }
    }
}
