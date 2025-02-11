using Euphonia.Common;

namespace Euphonia.API.Models.Data
{
    public class DownloadSongData
    {
        public Song Song { set; get; }
        public DownloadState CurrentState { set; get; }
        public string Error { set; get; }
        public DateTime LastUpdate { set; get; }

        public string DownloadUrl { set; get; }
        public string RawPath { set; get; }
        public string NormPath { set; get; }
    }

    public enum DownloadState
    {
        Downloading,
        Normalizing,
        Finished
    }
}
