namespace Euphonia.API.Models.Request;

public class SongFormPatchPlaylist : SongIdentifier
{
    public string[] Playlists { set; get; } = [];
}
