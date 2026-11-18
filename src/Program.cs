namespace WarHack;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path.StartsWithSegments("/swagger"))
                {
                    var auth = ctx.RequestServices.GetRequiredService<IAuthorizationService>();
                    var result = await auth.AuthorizeAsync(ctx.User, resource: null, policyName: "IsAdmin");

                    if (!result.Succeeded)
                    {
                        await ctx.ChallengeAsync(IdentityConstants.ApplicationScheme);
                        return;
                    }
                }

                await next();
            });

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"{path.SwaggerPath}/swagger.json", "WarHackV1");
                c.RoutePrefix = "swagger";
            });
        }

        app.MapDefaultControllerRoute();
        app.MapRazorPages();
        app.MapGet("/welcome", (ClaimsPrincipal user) => $"Hello {user.Identity!.Name}")
           .RequireAuthorization();

        await app.RunAsync();
    }
}
