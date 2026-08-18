using WarHack.Infrastructure.Models;

namespace WarHack.Infrastructure.Models;

public class HttpResponseData<T> : HttpResponseMessage where T : class
{
    private bool IsSuccess { get; }
    private string Message { get; }
    private T ResponseContent { get; init; }

    private HttpResponseData(bool issuccess, string msg, T content)
    {
        IsSuccess = issuccess;
        Message = msg;
        ResponseContent = content;
    }
}