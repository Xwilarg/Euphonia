using Euphonia.API.Models.Data;

namespace Euphonia.API.Models.Response;

public class SongDownloadResponse : BaseResponse
{
    public required SongDownloadData[] Data { set; get; }
}

public class SongDownloadData
{
    public required string SongName { set; get; }
    public required string? SongArtist { set; get; }
    public required DownloadState CurrentState { set; get; }
    public required string? Error { set; get; }
}
