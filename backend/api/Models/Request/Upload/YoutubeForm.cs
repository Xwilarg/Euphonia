using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models.Request.Upload;

public class YoutubeForm: AUploadForm
{
    [Required]
    public string Youtube { set; get; }
}
