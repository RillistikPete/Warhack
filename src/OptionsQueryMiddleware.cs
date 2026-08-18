using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using WarHack.Controllers;
using Microsoft.Extensions.Options;
using WarHack.Identity;

namespace WarHack;

public class OptionsQueryMiddleware
{
    private readonly RequestDelegate _next;

    public OptionsQueryMiddleware(RequestDelegate next, IConfiguration iconfig)
    {
        _next = next;
    }

    // Test with https://localhost:5001/Privacy/?option=Hello
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