using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models;

public class PlaylistForm
{
    [Required]
    public string Name { set; get; }
    public string FullName { set; get; }
    public string? Description { set; get; } = string.Empty;
    public string? ImageUrl { set; get; } = string.Empty;
}
