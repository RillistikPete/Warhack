using Microsoft.Extensions.Options;
using WarHack.Domain;

namespace WarHack.Blizzard.Auth;

/// <summary>
/// Holds the current token for handler lifetime
/// </summary>
public sealed class BlizzardTokenCache
{
    public SemaphoreSlim Semaphore { get; } = new(1, 1);
    public string? AccessToken { get; set; }
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.MinValue;

    public bool IsValid => AccessToken is not null && DateTimeOffset.UtcNow < ExpiresAt;

    public void Clear()
    {
        AccessToken = null;
        ExpiresAt = DateTimeOffset.MinValue;
    }
}

public sealed class BlizzardTokenProvider : IBlizzardTokenProvider
{
    private readonly IBlizzardTokenClient _client;
    private readonly BlizzardTokenCache _cache;
    private readonly BlizzardOptions _options;
    private readonly ILogger<BlizzardTokenProvider> _log;

    public BlizzardTokenProvider(
        IBlizzardTokenClient client,
        BlizzardTokenCache cache,
        IOptions<BlizzardOptions> options,
        ILogger<BlizzardTokenProvider> log)
    {
        _client = client;
        _cache = cache;
        _options = options.Value;
        _log = log;
    }

    public void ClearToken() => _cache.Clear();

    public async Task<ApiResult<string>> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_cache.IsValid)
        {
            return ApiResult<string>.Ok(_cache.AccessToken!);
        }

        await _cache.Semaphore.WaitAsync(ct);
        try
        {
            // Another caller may have refreshed while we queued
            if (_cache.IsValid)
            {
                return ApiResult<string>.Ok(_cache.AccessToken!);
            }

            var result = await _client.RequestTokenAsync(ct);
            if (!result.IsSuccess || result.Value is null)
            {
                return ApiResult<string>.Fail(result.Error ?? "Token request failed.", result.Status);
            }

            _cache.AccessToken = result.Value.AccessToken;
            _cache.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(result.Value.ExpiresIn).Subtract(_options.TokenRefreshTime);

            _log.LogInformation("Acquired Blizzard token, valid until {ExpiresAt:u}.", _cache.ExpiresAt);
            return ApiResult<string>.Ok(_cache.AccessToken);
        }
        finally
        {
            _cache.Semaphore.Release();
        }
    }
}
