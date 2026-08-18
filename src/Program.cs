namespace WarHack;

using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using WarHack.Identity;
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        StartupOptions.ConfigureServices(builder.Services, builder.Configuration);

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

/*
public class ConfigurationMgmt(IConfiguration config) : IConfigurationStore
{
    private readonly IConfiguration _config = config;

    public string GetConfiguration<T>() where T : new()
    {
        const string fnName = nameof(this.GetConfiguration);
        var sec = _config.Get<T>();
        //return sec.GetProperties();
        // var typee = t.GetType();
        // return 
        return "";
    
    }
}

public class ConfigurationMgmt(IConfigurationRoot config) : IConfigurationStore
{
    private readonly IConfigurationRoot _cr = config;
    
    //OLD
    public string GetSectionKV(string section, string key)
    {
        const string fnName = nameof(this.GetSectionKV);
        string? value = _cr.GetValue<string>(section + ":" + key);

        // Configuration config =
        //     ConfigurationManager.OpenExeConfiguration(ConfigurationUsesrLevel.None);

        // AppSettingsSection appSettings =
        //     (AppSettingsSection)config.GetSection(key);
        
        if(!string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
        else
        {
            return $"{fnName}-key:{key}|value: NULL";
        }
    }
   
}
*/
