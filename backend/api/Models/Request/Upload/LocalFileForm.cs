using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models.Request.Upload;

public class LocalFileForm : AUploadForm
{
    [Required]
    public IFormFile LocalFile { set; get; }
}
