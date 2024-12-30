namespace Euphonia.API.Models.Response;

public class SongResponse : BaseResponse
{
    public string[] Tags { set; get; }
    public string AlbumKey { set; get; }
    public string AlbumSource { set; get; }
    public string AlbumPath { set; get; }
    public string AlbumName { set; get; }
    public string Name { set; get; }
    public string Artist { set; get; }
}
