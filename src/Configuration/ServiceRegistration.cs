using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using WarHack.Application;
using WarHack.Blizzard;
using WarHack.Blizzard.Auth;
using WarHack.Data;
using WarHack.Http;
using WarHack.Identity;
using WarHack.Logging;
using WarHack.Middleware;
using WarHack.Options;

namespace WarHack.Configuration
{
    /// <summary>
    /// Every service this app registers, in one place, behind one call from
    /// Program.cs. Grouped by concern rather than by DI lifetime so the shape of
    /// the application is readable from top to bottom.
    /// </summary>
    public static class ServiceRegistration
    {
        public static IServiceCollection AddWarHackServices(
            this IServiceCollection svcCollection, IConfiguration cf)
        {

            svcCollection.AddTransient<IStartupFilter, Intercessors>();
            svcCollection.Configure<HomeControllerSettings>(cf.GetSection("AppSettings"));

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

            // Character views are cached in-process; SizeLimit makes the cache
            // bounded rather than "until the app runs out of memory".
            svcCollection.AddMemoryCache(o => o.SizeLimit = 1024);
            svcCollection.AddScoped<IAthenaeum, Athenaeum>();

            svcCollection.AddAuthorization(o =>
            {
                o.AddPolicy("IsAdmin", p => p.RequireRole("Admin"));
                o.AddPolicy("HasBattleTag", p => p.RequireClaim("battletag"));
            });

            // ── Logging ────────────────────────────────────────────────────────────
            // Callers hand rows to a channel; a background writer batches them into
            // Postgres. Nothing on the request path waits for the database.
            svcCollection.AddSingleton<LogShipChannel>();
            svcCollection.AddHostedService<LogShipWriter>();
            svcCollection.AddScoped<ILogShipHarbor, Logship>();

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

            // ── Register clientel ──────────────────────────────────────────────────
            svcCollection.AddOptions<BlizzardOptions>()
                .Bind(cf.GetSection(BlizzardOptions.SectionName))
                .Validate(o => !string.IsNullOrWhiteSpace(o.ClientId), "Blizzard:ClientId is required.")
                .Validate(o => !string.IsNullOrWhiteSpace(o.ClientSecret), "Blizzard:ClientSecret is required.")
                .Validate(o => Uri.IsWellFormedUriString(o.BaseUrl, UriKind.Absolute), "Blizzard:BaseUrl must be an absolute URI.")
                .ValidateOnStart(); // Validate on startup, or else get creds error on first request.

            // Handlers are resolved per client instance, so they register transient.
            svcCollection.AddTransient<AsyncRequestHost>();
            svcCollection.AddTransient<BlizzardAuthHandler>();

            // OAuth: the cache is a singleton so one token is shared process-wide and survives handler rotation.
            svcCollection.AddSingleton<BlizzardTokenCache>();
            svcCollection.AddTransient<IBlizzardTokenProvider, BlizzardTokenProvider>();
            svcCollection.AddHttpClient<IBlizzardTokenClient, BlizzardTokenClient>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(15);
            });

            svcCollection.AddHttpClient<IBlizzardClient, BlizzardClient>((sp, c) =>
            {
                var blizzard = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BlizzardOptions>>().Value;
                c.BaseAddress = new Uri(blizzard.BaseUrl);
                c.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            // Handlers run outermost-first in the order they are added:
            //   timing  ->  auth  ->  resilience  ->  network
            .AddHttpMessageHandler<AsyncRequestHost>()
            .AddHttpMessageHandler<BlizzardAuthHandler>()
            .AddStandardResilienceHandler(o =>
            {
                // Defaults already retry 5xx, 408 and 429 with exponential backoff
                // and jitter, so there is no status-code list to maintain by hand.
                o.Retry.MaxRetryAttempts = 3;
                o.Retry.Delay = TimeSpan.FromMilliseconds(500);

                // Per attempt. Blizzard is usually fast; fail early and retry.
                o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);

                // Ceiling across all attempts.
                o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);

                // Must be at least 2x AttemptTimeout or startup validation throws.
                o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                o.CircuitBreaker.FailureRatio = 0.5;
                o.CircuitBreaker.MinimumThroughput = 10;
            });

            // The old Polly v7 RetryPolicy()/FallbackPolicy()/statusCodesForRetry
            // are gone - the standard handler above covers retry, circuit breaker
            // and timeouts. Retry/breaker events surface through ILogger telemetry
            // rather than a custom onRetry callback.

            return svcCollection;
        }
    }
}
