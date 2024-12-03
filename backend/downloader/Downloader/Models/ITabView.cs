using Downloader.ViewModels;

namespace Downloader.Models;

public interface ITabView
{
    public void AfterInit();
    public void OnDataRefresh();
}
