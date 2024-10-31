using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models
{
    public class SongIdentifier
    {
        [Required]
        public string Key { set; get; }
    }
}
