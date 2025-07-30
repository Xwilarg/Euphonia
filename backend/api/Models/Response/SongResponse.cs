namespace Euphonia.API.Models.Response;

public class SongResponse : BaseResponse
{
    public required string[] Tags { set; get; }
    public required string Name { set; get; }
    public required string Artist { set; get; }
    public required string? Thumnail { set; get; }
}
