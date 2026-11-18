using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
using WarHack.Security;

namespace WarHack.Configuration
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddWarHackServices(this IServiceCollection svcCollection, IConfiguration cf)
        {
            svcCollection.AddTransient<IStartupFilter, Intercessors>();

            svcCollection.AddControllersWithViews();
            svcCollection.AddRazorPages();

            // require api key 
            svcCollection.AddOptions<ApiKeyOptions>(ApiKeyDefaults.Scheme)
                .Bind(cf.GetSection(ApiKeyOptions.SectionName))
                .Validate(o => o.Clients.Values.Any(x => !string.IsNullOrWhiteSpace(x.Key)), "No header API keys configured.")
                .ValidateOnStart();

            svcCollection.AddAuthentication()
                .AddScheme<ApiKeyOptions, ApiKeyAuthenticationHandler>(ApiKeyDefaults.Scheme, _ => { });

            svcCollection.AddAuthorizationBuilder()
                .AddPolicy("IsAdmin", p => p.RequireRole("Admin"))
                .AddPolicy("HasBattleTag", p => p.RequireClaim("battletag"));

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

            svcCollection.ConfigureApplicationCookie(o =>
            {
                o.LoginPath = "/Account/Login";
                o.AccessDeniedPath = "/Home/AccessDenied";
                o.LogoutPath = "/Account/Logout";
            });

            svcCollection.AddScoped<IdentitySeeder>();
            svcCollection.AddScoped<IUserClaimsPrincipalFactory<WarHackUser>, AppUserClaimsPrincipalFactory>();

            svcCollection.AddMemoryCache(o => o.SizeLimit = 1024);
            svcCollection.AddScoped<IAthenaeum, Athenaeum>();

            // Logging
            svcCollection.AddSingleton<LogShipChannel>();
            svcCollection.AddHostedService<LogShipWriter>();
            svcCollection.AddSingleton<ILogShipHarbor, LogShipHarbor>();
            svcCollection.Configure<LogRetentionOptions>(cf.GetSection(LogRetentionOptions.SectionName));
            svcCollection.AddHostedService<LogRetentionService>();

            svcCollection.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("apikey", new OpenApiSecurityScheme
                {
                    Description = "Swagger API key required for authorization. Sent as the x-api-key header.",
                    Type = SecuritySchemeType.ApiKey,
                    Name = "x-api-key",
                    In = ParameterLocation.Header
                });
                options.OperationFilter<SwaggerApiSecurityOperationFilter>();
            });

            svcCollection.AddOptions<BlizzardOptions>()
                .Bind(cf.GetSection(BlizzardOptions.SectionName))
                .Validate(o => !string.IsNullOrWhiteSpace(o.ClientId), "Blizzard:ClientId is required.")
                .Validate(o => !string.IsNullOrWhiteSpace(o.ClientSecret), "Blizzard:ClientSecret is required.")
                .Validate(o => Uri.IsWellFormedUriString(o.BaseUrl, UriKind.Absolute), "Blizzard:BaseUrl must be an absolute URI.")
                .ValidateOnStart();

            svcCollection.AddTransient<AsyncRequestHost>();
            svcCollection.AddTransient<BlizzardAuthHandler>();

            svcCollection.AddHttpClient(BlizzardTokenService.HttpClientName, c =>
            {
                c.Timeout = TimeSpan.FromSeconds(15);
            });
            svcCollection.AddSingleton<IBlizzardTokenService, BlizzardTokenService>();
            svcCollection.AddHostedService<BlizzardTokenRetriever>();

            svcCollection.AddHttpClient<IBlizzardClient, BlizzardClient>((sp, c) =>
            {
                var bOptions = sp.GetRequiredService<IOptions<BlizzardOptions>>().Value;
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
