using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models.Request;

public class PlaylistForm
{
    [Required]
    public required string Name { set; get; }
    public string? FullName { set; get; } = null;
    public string? Description { set; get; } = string.Empty;
    public string? ImageUrl { set; get; } = null;
}
