using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models;

public class YoutubeForm
{
    [Required]
    public string Name { set; get; }
    public string? Artist { set; get; } = string.Empty;
    [Required]
    public string Youtube { set; get; }
    public string? AlbumName { set; get; } = string.Empty;
    public string? AlbumUrl { set; get; } = string.Empty;
    public string? SongType { set; get; } = string.Empty;
    public string? Playlist { set; get; } = string.Empty;
}
