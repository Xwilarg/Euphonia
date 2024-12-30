using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models;

public class SongToggleAction : SongIdentifier
{
    [Required]
    public bool IsOn { set; get; }
}
