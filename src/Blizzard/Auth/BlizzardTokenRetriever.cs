using Microsoft.Extensions.Options;
using WarHack.Blizzard.Interfaces;

namespace WarHack.Blizzard.Auth;

/// <summary>
/// Keeps a valid Blizzard token in hand at all times
/// Refreshes every 2 minutes
/// </summary>
public sealed class BlizzardTokenRetriever : BackgroundService
{
    private readonly IBlizzardTokenService _tokenService;
    private readonly BlizzardOptions _options;
    private readonly ILogger<BlizzardTokenRetriever> _log;

    public BlizzardTokenRetriever(IBlizzardTokenService tokenService, IOptions<BlizzardOptions> options, ILogger<BlizzardTokenRetriever> log)
    {
        _tokenService = tokenService;
        _options = options.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (await _tokenService.GetAccessTokenAsync(cancellationToken) is null)
            {
                _log.LogWarning("Blizzard token warm-up failed at startup. Next request will get a new one.");
            }

            using var timer = new PeriodicTimer(_options.TokenRefreshInterval);

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (!await _tokenService.RefreshAsync(cancellationToken))
                {
                    _log.LogWarning("keeping the current token.");
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            _log.LogError(ex, "Blizzard token warm-up stopped unexpectedly. Lazy token acquisition still applies.");
        }
    }
}
