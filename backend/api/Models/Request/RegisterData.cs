using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models.Request
{
    public class RegisterData
    {
        [Required]
        public string Key { set; get; }
        [Required]
        public string Path { set; get; }
    }
}
