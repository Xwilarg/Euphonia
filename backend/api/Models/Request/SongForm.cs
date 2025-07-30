using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models.Request;

public class SongForm : SongIdentifier
{
    [Required]
    public required string Name { set; get; }
    public string Artist { set; get; } = string.Empty;
    public string[] Playlists { set; get; } = [];
    public string[]? Tags { set; get; } = [];
    public string Source { set; get; } = string.Empty;
    public string? CoverUrl { set; get; } = null;
}
