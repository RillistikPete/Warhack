using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace WarHack.Middleware;

public class OptionsQueryMiddleware
{
    private readonly RequestDelegate _next;

    public OptionsQueryMiddleware(RequestDelegate next, IConfiguration iconfig)
    {
        _next = next;
    }
    public async Task Invoke(HttpContext httpContext)
    {
        var option = httpContext.Request.Query["option"];

        if (!string.IsNullOrWhiteSpace(option))
        {
            httpContext.Items["option"] = WebUtility.HtmlEncode(option);
        }

        await _next(httpContext);
    }

}