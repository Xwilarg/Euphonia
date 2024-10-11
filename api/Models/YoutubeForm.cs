using System.ComponentModel.DataAnnotations;

namespace Euphonia.API.Models;

public class YoutubeForm
{
    [Required]
    public string Name { set; get; }
    public string Artist { set; get; }
    [Required]
    public string Youtube { set; get; }
    public string AlbumName { set; get; }
    public string AlbumUrl { set; get; }
    public string SongType { set; get; }
}
