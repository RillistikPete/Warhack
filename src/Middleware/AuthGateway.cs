
namespace WarHack.Middleware;

public class AuthGateway
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthGateway> _logger;

    public AuthGateway(RequestDelegate next, ILogger<AuthGateway> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (_logger.IsEnabled(LogLevel.Debug) && httpContext.Request.Cookies.Count > 0)
        {
            _logger.LogDebug("Request cookies: {CookieNames}",
                string.Join(", ", httpContext.Request.Cookies.Keys));
        }

        await _next(httpContext);
    }
}

public static class AuthGatewayExtensions
{
    public static IApplicationBuilder UseAuthGateway(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthGateway>();
    }
}