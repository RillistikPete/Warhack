using System.Diagnostics;

namespace WarHack.Http;

/// <summary>
/// Outbound request timing/diagnostics.
/// </summary>
public sealed class AsyncRequestHost : DelegatingHandler
{
    private readonly ILogger<AsyncRequestHost> _log;

    public AsyncRequestHost(ILogger<AsyncRequestHost> log) => _log = log;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage reqMsg, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(reqMsg, cancellationToken);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                _log.LogDebug("{Method} {Uri} -> {Status} in {Elapsed}ms",
                    reqMsg.Method, reqMsg.RequestUri, (int)response.StatusCode, sw.ElapsedMilliseconds);
            }
            else
            {
                _log.LogWarning("{Method} {Uri} -> {Status} in {Elapsed}ms",
                    reqMsg.Method, reqMsg.RequestUri, (int)response.StatusCode, sw.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            // Log and rethrow.
            _log.LogError(ex, "{Method} {Uri} threw after {Elapsed}ms",
                reqMsg.Method, reqMsg.RequestUri, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
