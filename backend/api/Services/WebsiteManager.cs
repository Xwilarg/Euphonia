using Euphonia.API.Models.Data;
using System.Text.Json;

namespace Euphonia.API.Services
{
    public class WebsiteManager
    {
        public string? GetPath(string key)
        {
            if (_endpoints.TryGetValue(key, out string? value)) return value;
            return null;
        }

        public void Add(string key, string path)
        {
            if (!path.EndsWith('/') && !path.EndsWith('\\')) path += '/';
            _endpoints.Add(key, path);
        }

        public string AdminTokenLookup(string hashedPwd)
        {
            if (string.IsNullOrWhiteSpace(hashedPwd)) return null;
            foreach (var e in _endpoints)
            {
                if (JsonSerializer.Deserialize<Credentials>(File.ReadAllText(e.Value + "credentials.json"), new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })?.AdminPwd == hashedPwd)
                {
                    return e.Key;
                }
            }
            return null;
        }

        private readonly Dictionary<string, string> _endpoints = [];
    }
}
