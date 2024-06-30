
namespace DiscordBot.Data;

#nullable disable
public class JsonExportData
{
    public string[] Highlight { set; get; } = [];
    public Dictionary<string, Playlist> Playlists { set; get; } = [];
    public Song[] Musics { set; get; } = [];
    public Dictionary<string, Album> Albums { set; get; } = [];
}

public class Album
{
    public string Path { set; get; }
}

public class Song
{
    public string Name { set; get; }
    public string Path { set; get; }
    public string Artist { set; get; }
    public string Album { set; get; }
    public string Playlist { set; get; }
    public string Source { set; get; }
    public string Type { set; get; }
}

public class Playlist
{
    public string Name { set; get; }
    public string Description { set; get; }
}
#nullable enable