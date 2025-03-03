using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models.Request.Upload;

public class AUploadForm
{
    [Required]
    public string Name { set; get; }
    public string? Artist { set; get; } = string.Empty;
    public string? AlbumName { set; get; } = string.Empty;
    public string? AlbumUrl { set; get; } = string.Empty;
    public string? SongType { set; get; } = string.Empty;
    public string? Playlist { set; get; } = string.Empty;
}
