using Euphonia.API.Models.Data;

namespace Euphonia.API.Models.Response;

public class SongDownloadResponse : BaseResponse
{
    public SongDownloadData[] Data { set; get; }
}

public class SongDownloadData
{
    public string SongName { set; get; }
    public string SongArtist { set; get; }
    public DownloadState CurrentState { set; get; }
    public string Error { set; get; }
}
