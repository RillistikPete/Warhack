namespace WarHack;

using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using WarHack.Configuration;
using WarHack.Identity;
using WarHack.Options;
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddWarHackServices(builder.Configuration);

        var path = builder.Configuration
            .GetSection(nameof(AppConfigOptions))
            .Get<AppConfigOptions.SwaggerConfigOptions>()
            ?? throw new InvalidOperationException("Swagger config missing.");

        var app = builder.Build();

        await app.SeedIdentityAsync();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseWhen(
            ctx => !ctx.Request.Path.StartsWithSegments("/api"),
            branch => branch.UseStatusCodePagesWithReExecute("/Home/Error/{0}"));
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"{path.SwaggerPath}/swagger.json", "WarHackV1");
                c.RoutePrefix = "swagger";
            });
        }

        app.MapDefaultControllerRoute();
        app.MapRazorPages();
        app.MapGet("/claimWelcome", (ClaimsPrincipal user) => $"Hello {user.Identity!.Name}")
           .RequireAuthorization();

        await app.RunAsync();
    }
}
