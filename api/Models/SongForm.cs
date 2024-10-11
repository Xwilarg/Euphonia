using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models;

public class SongForm
{
    [Required]
    public string Key { set; get; }
    public string[]? Tags { set; get; } = [];
}
