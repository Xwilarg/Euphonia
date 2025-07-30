using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models.Request
{
    public class SongIdentifier
    {
        [Required]
        public required string Key { set; get; }
    }
}
