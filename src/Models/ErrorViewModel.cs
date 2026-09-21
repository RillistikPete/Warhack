namespace WarHack.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public int StatusCode { get; set; }
    public string? OriginalPath { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public string Title => StatusCode switch
    {
        404 => "Oops! That page doesn't exist.",
        403 => "You don't have the required permissions to access this page.",
        401 => "You need to sign in to do that.",
        500 => "Server error.",
        _ => "Something went wrong"
    };
}
