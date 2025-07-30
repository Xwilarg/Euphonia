namespace Euphonia.API.Models.Response;

public class BaseResponse
{
    public required bool Success { set; get; }
    public required string? Reason { set; get; }
}
