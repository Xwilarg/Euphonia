namespace Euphonia.API.Models.Data
{
    public class GeniusData
    {
        public GeniusResponse Response { set; get; }
    }

    public class GeniusResponse
    {
        public GeniusHit[] Hits { set; get; }
    }

    public class GeniusHit
    {
        public string Type { set; get; }
        public GeniusContent Result { set; get; }
    }

    public class GeniusContent
    {
        public string FullTitle { set; get; }
        public string ArtistNames { set; get; }
        public int Id;
    }
}
