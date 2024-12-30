using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models;

public class SongForm : SongIdentifier
{
    [Required]
    public string Name { set; get; }
    public string Artist { set; get; } = string.Empty;
    public string[]? Tags { set; get; } = [];
    public string Source { set; get; } = string.Empty;
}
