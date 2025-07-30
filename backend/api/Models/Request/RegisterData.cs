using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models.Request
{
    public class RegisterData
    {
        [Required]
        public required string Key { set; get; }
        [Required]
        public required string Path { set; get; }
    }
}
