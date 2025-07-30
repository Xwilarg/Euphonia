using Euphonia.Common;

namespace Euphonia.API.Models.Data
{
    public class DownloadSongData
    {
        public required Song Song { set; get; }
        public required DownloadState CurrentState { set; get; }
        public required string? Error { set; get; }
        public required DateTime LastUpdate { set; get; }

        public required string? DownloadUrl { set; get; }
        public required string RawPath { set; get; }
        public required string? NormPath { set; get; }
    }

    public enum DownloadState
    {
        Downloading,
        Normalizing,
        Finished
    }
}
