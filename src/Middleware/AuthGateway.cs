
using System.Diagnostics;
using WarHack.Logging;

namespace WarHack.Middleware;


public class AuthGateway
{
    private readonly RequestDelegate _next;

    public AuthGateway(RequestDelegate next)
    {
        _next = next;
    }

    // IMessageWriter is injected into InvokeAsync
    public async Task InvokeAsync(HttpContext httpContext, ILogShipHarbor ship)
    {
        if(httpContext.Request.Cookies.Count > 0)
        {
            foreach(var i in httpContext.Request.Cookies)
            {
                Debug.WriteLine($"\\n httpcontext request cookie - Key: {i.Key} - Val: {i.Value}");
            }
        }
        ship.PrintToOut("Lovely day " + DateTime.Now.ToShortDateString());
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