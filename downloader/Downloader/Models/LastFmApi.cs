using System.Text.Json.Serialization;

namespace Downloader.Models
{
    public class LastFmApi
    {
        public LastFmTrack Track { set; get; }
        public string Message { set; get; } // Only present if an error occured
    }

    public class LastFmTrack
    {
        public LastFmAlbum Album { set; get; }
    }

    public class LastFmAlbum
    {
        public LastFmImage[] Image { set; get; }
        public string Title { set; get; }
    }

    public class LastFmImage
    {
        [JsonPropertyName("#text")]
        public string Text { set; get; }
}
