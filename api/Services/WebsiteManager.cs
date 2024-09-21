namespace Euphonia.API.Services
{
    public class WebsiteManager
    {

        public List<string> Endpoints { set; get; } = new();
        public Dictionary<string, WebsiteToken> Tokens { set; get; } = new();

        public class WebsiteToken
        {
            public DateTime Expiration { set; get; }
            public string Path { set; get; }
        }
    }
}
