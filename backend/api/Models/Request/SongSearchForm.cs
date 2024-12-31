using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models.Request;

public class SongSearchForm
{
    [Required]
    public string Name { set; get; }
    public string Artist { set; get; } = string.Empty;
}
