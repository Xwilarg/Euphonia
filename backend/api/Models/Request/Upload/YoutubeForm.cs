using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models.Request.Upload;

public class YoutubeForm: AUploadForm
{
    [Required]
    public required string Youtube { set; get; }
}
