using System.Text.Encodings.Web;
using System.Text.Json;

namespace Euphonia.Common;

public static class Serialization
{
    private static JsonSerializerOptions _option;
    private static JsonSerializerOptions Option
    {
        get
        {
            _option ??= new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return _option;
        }
    }

    public static T Deserialize<T>(string text) => JsonSerializer.Deserialize<T>(text, Option);
    public static string Serialize<T>(T data) => JsonSerializer.Serialize(data, Option);
}

public class EuphoniaCredentials
{
    public string? AdminPwd { set; get; } = null;
}

public class EuphoniaMetadata
{
    public string[] Readme { set; get; } = [];
    public bool ShowGithub { set; get; } = false;
    public bool ShowDebug { set; get; } = false;
    public bool AllowDownload { set; get; } = false;
    public bool AllowShare { set; get; } = false;
    public bool ShowAllPlaylist { set; get; } = false;
}

public class EuphoniaInfo
{
    public Dictionary<string, Playlist> Playlists { set; get; } = [];
    public List<Song> Musics { set; get; } = [];
    public Dictionary<string, Album> Albums { set; get; } = [];
    public string[] Tags { set; get; } = [];
}

public class Album
{
    public string Path { set; get; }
    public string Name { set; get; }
    public string Source { set; get; }
}

public class Song
{
    public string Key { set; get; }
    public string Name { set; get; }
    public string Path { set; get; }
    public string Artist { set; get; }
    public string Album { set; get; }
    public string Playlist { set; get; }
    public string Source { set; get; }
    public string Type { set; get; }
    public string[] Tags { set; get; } = [];
    public bool IsArchived { set; get; }
    public bool IsFavorite { set; get; }
}

public class Playlist
{
    public string Name { set; get; }
    public string Description { set; get; }
    public string ImageUrl { set; get; }
}
