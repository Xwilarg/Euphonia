using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models.Request.Upload;

public class AUploadForm
{
    [Required]
    public required string Name { set; get; }
    public string? Artist { set; get; } = string.Empty;
    public string? AlbumName { set; get; } = string.Empty;
    public string? CoverUrl { set; get; } = string.Empty;
    public string? SongType { set; get; } = string.Empty;
    public string[] Playlists { set; get; } = [];
}
