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
    public string Name { set; get; } = "Euphonia";
    public string[] Readme { set; get; } = [];
    public bool ShowGithub { set; get; } = true;
    public bool ShowDebug { set; get; } = true;
    public bool AllowDownload { set; get; } = true;
    public bool AllowShare { set; get; } = true;
    public bool ShowAllPlaylist { set; get; } = true;
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
    /// <summary>
    /// Unique song identifier
    /// </summary>
    public string Key { set; get; }
    /// <summary>
    /// Name of the song
    /// </summary>
    public string Name { set; get; }
    /// <summary>
    /// Name of the song under its raw format
    /// </summary>
    public string RawPath { set; get; }
    /// <summary>
    /// Name of the song under its normalized format
    /// </summary>
    public string Path { set; get; }
    /// <summary>
    /// Artists of the song
    /// </summary>
    public string Artist { set; get; }
    /// <summary>
    /// Key of the album of the song
    /// </summary>
    public string Album { set; get; }
    /// <summary>
    /// Which playlists is this song stored in
    /// </summary>
    public string[] Playlists { set; get; } = [];
    /// <summary>
    /// Where is this song coming from (YouTube link of "localfile")
    /// </summary>
    public string Source { set; get; }
    /// <summary>
    /// Type of the song (cover, instrumental, etc...)
    /// </summary>
    public string Type { set; get; }
    /// <summary>
    /// User tags attached to the song
    /// </summary>
    public string[] Tags { set; get; } = [];
    /// <summary>
    /// Archived songs are songs that we don't display to the users anymore
    /// </summary>
    public bool IsArchived { set; get; }
    /// <summary>
    /// If the song was favorited by the user
    /// </summary>
    public bool IsFavorite { set; get; }
}

public class Playlist
{
    public string Name { set; get; }
    public string Description { set; get; }
    public string ImageUrl { set; get; }
}
