using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using WarHack.Application;
using WarHack.Blizzard;
using WarHack.Blizzard.Auth;
using WarHack.Blizzard.Interfaces;
using WarHack.Data;
using WarHack.Http;
using WarHack.Identity;
using WarHack.Logging;
using WarHack.Middleware;

namespace WarHack.Configuration
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddWarHackServices(this IServiceCollection svcCollection, IConfiguration cf)
        {
            svcCollection.AddTransient<IStartupFilter, Intercessors>();

            svcCollection.AddControllersWithViews();
            svcCollection.AddRazorPages();
            svcCollection.AddAuthentication().AddBearerToken(IdentityConstants.BearerScheme);
            svcCollection.AddAuthorizationBuilder();

            var dbConn = cf.GetConnectionString("WarHackDb");
            svcCollection.AddDbContext<WarHackDbContext>(db => db.UseNpgsql(dbConn));

            svcCollection.AddIdentity<WarHackUser, IdentityRole>(o =>
            {
                o.Password.RequiredLength = 12;
                o.Password.RequiredUniqueChars = 0;
                o.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<WarHackDbContext>()
            .AddRoles<IdentityRole>()
            .AddDefaultTokenProviders();

            svcCollection.AddScoped<IdentitySeeder>();
            svcCollection.AddScoped<IUserClaimsPrincipalFactory<WarHackUser>, AppUserClaimsPrincipalFactory>();

            svcCollection.AddMemoryCache(o => o.SizeLimit = 1024);
            svcCollection.AddScoped<IAthenaeum, Athenaeum>();

            svcCollection.AddAuthorization(o =>
            {
                o.AddPolicy("IsAdmin", p => p.RequireRole("Admin"));
                o.AddPolicy("HasBattleTag", p => p.RequireClaim("battletag"));
            });

            // Logging
            svcCollection.AddSingleton<LogShipChannel>();
            svcCollection.AddHostedService<LogShipWriter>();
            svcCollection.AddSingleton<ILogShipHarbor, LogShipHarbor>();
            svcCollection.Configure<LogRetentionOptions>(cf.GetSection(LogRetentionOptions.SectionName));
            svcCollection.AddHostedService<LogRetentionService>();

            // Swagger
            svcCollection.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("apikey", new OpenApiSecurityScheme
                {
                    Description = "Swagger API Security WH",
                    Type = SecuritySchemeType.ApiKey,
                    Name = "x-api-key",
                    In = ParameterLocation.Header
                });
                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("apikey", document)] = []
                });
            });

            svcCollection.AddOptions<BlizzardOptions>()
                .Bind(cf.GetSection(BlizzardOptions.SectionName))
                .Validate(o => !string.IsNullOrWhiteSpace(o.ClientId), "Blizzard:ClientId is required.")
                .Validate(o => !string.IsNullOrWhiteSpace(o.ClientSecret), "Blizzard:ClientSecret is required.")
                .Validate(o => Uri.IsWellFormedUriString(o.BaseUrl, UriKind.Absolute), "Blizzard:BaseUrl must be an absolute URI.")
                .ValidateOnStart(); // will throw on startup, or else get creds error on first request.

            svcCollection.AddTransient<AsyncRequestHost>();
            svcCollection.AddTransient<BlizzardAuthHandler>();

            // Register clientel
            svcCollection.AddHttpClient(BlizzardTokenService.HttpClientName, c =>
            {
                c.Timeout = TimeSpan.FromSeconds(15);
            });
            svcCollection.AddSingleton<IBlizzardTokenService, BlizzardTokenService>();

            svcCollection.AddHttpClient<IBlizzardClient, BlizzardClient>((sp, c) =>
            {
                var bOptions = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BlizzardOptions>>().Value;
                c.BaseAddress = new Uri(bOptions.BaseUrl);
                c.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddHttpMessageHandler<AsyncRequestHost>()
            .AddHttpMessageHandler<BlizzardAuthHandler>()
            .AddStandardResilienceHandler(o =>
            {
                o.Retry.MaxRetryAttempts = 3;
                o.Retry.Delay = TimeSpan.FromMilliseconds(500);

                o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);

                o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);

                o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                o.CircuitBreaker.FailureRatio = 0.5;
                o.CircuitBreaker.MinimumThroughput = 10;
            });

            return svcCollection;
        }
    }
}
