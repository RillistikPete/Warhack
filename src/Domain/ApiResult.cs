namespace WarHack.Domain;

public readonly record struct ApiResult<T>(bool IsSuccess, T? Value, int Status, string? Error)
{
    public static ApiResult<T> Ok(T value, int status = 200) => new(true, value, status, null);
    public static ApiResult<T> Fail(string error, int status = 0) => new(false, default, status, error);
    public T ValueOrThrow => IsSuccess && Value is not null
        ? Value
        : throw new InvalidOperationException(Error ?? "Request failed.");
}

public readonly record struct ApiResult(bool IsSuccess, int Status, string? Error)
{
    public static ApiResult Ok(int status = 200) => new(true, status, null);
    public static ApiResult Fail(string error, int status = 0) => new(false, status, error);
}
