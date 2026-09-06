namespace WarHack.Domain;

/// <summary>
/// The outcome of a call to an external API. Deliberately NOT derived from
/// HttpResponseMessage: this is a result, not a live HTTP response, so it owns
/// no streams and needs no disposal.
/// </summary>
public readonly record struct ApiResult<T>(bool IsSuccess, T? Value, int Status, string? Error)
{
    public static ApiResult<T> Ok(T value, int status = 200) => new(true, value, status, null);
    public static ApiResult<T> Fail(string error, int status = 0) => new(false, default, status, error);

    /// <summary>Value when successful; otherwise throws. Use only where failure is genuinely exceptional.</summary>
    public T ValueOrThrow => IsSuccess && Value is not null
        ? Value
        : throw new InvalidOperationException(Error ?? "Request failed.");
}

/// <summary>Non-generic outcome for calls with no response body (replaces the old ServerResponse).</summary>
public readonly record struct ApiResult(bool IsSuccess, int Status, string? Error)
{
    public static ApiResult Ok(int status = 200) => new(true, status, null);
    public static ApiResult Fail(string error, int status = 0) => new(false, status, error);
}
