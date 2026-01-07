using Euphonia.API.Models.Data;
using Euphonia.API.Services;

namespace Euphonia.API.Models.Response;

public class SongDownloadResponse : BaseResponse
{
    public required SongDownloadData[] Data { set; get; }
    public required ExportData? Export { set; get; }
}

public class SongDownloadData
{
    public required string SongName { set; get; }
    public required string? SongArtist { set; get; }
    public required DownloadState CurrentState { set; get; }
    public required string? Error { set; get; }
}

public class ExportData
{
    public required string? ExportPath { set; get; }
    public required ExportStatus Status { set; get; }
}
