using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models.Request
{
    public class SongIdentifier
    {
        [Required]
        public string Key { set; get; }
    }
}
