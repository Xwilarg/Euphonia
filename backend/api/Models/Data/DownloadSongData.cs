using Euphonia.Common;

namespace Euphonia.API.Models.Data
{
    public class DownloadSongData
    {
        public Song Song;
        public DownloadState CurrentState;
        public string Error;

        public string DownloadUrl;
        public string RawPath;
        public string NormPath;
    }

    public enum DownloadState
    {
        Downloading,
        Normalizing,
        Finished
    }
}
