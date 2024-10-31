namespace Euphonia.API.Models;

public class SongForm : SongIdentifier
{
    public string[]? Tags { set; get; } = [];
}
