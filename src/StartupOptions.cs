using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using WarHack.Identity;

namespace WarHack
{
    public class StartupOptions
    {
        internal static void ConfigureServices(IServiceCollection svcCollection, IConfiguration cf)
        {
            Console.WriteLine("StartupOptions > ConfigureServices");

            svcCollection.AddTransient<IStartupFilter, Intercessors>();
            svcCollection.Configure<HomeControllerSettings>(cf.GetSection("AppSettings"));
            // Add services to the container.

            //svcCollection.Configure<HomeController>()

            svcCollection.AddControllersWithViews();
            svcCollection.AddRazorPages();
            svcCollection.AddAuthentication().AddBearerToken(IdentityConstants.BearerScheme);
            svcCollection.AddAuthorizationBuilder();

            var dbConn = cf.GetConnectionString("WarHackDb");
            svcCollection.AddDbContext<ApplicationDbContext>(db => db.UseNpgsql(dbConn));

            //production db
            //var csb = new NpgsqlConnectionStringBuilder(config.GetConnectionString("WarHackDb"))
            //{
            //    Password = config["Db:Password"]   // env var
            //};

            //svcCollection.AddDbContext<ApplicationDbContext>(db => db.UseNpgsql(csb.ConnectionString));

            svcCollection.AddIdentity<WarHackUser, IdentityRole>(o =>
            {
                o.Password.RequiredLength = 12;
                o.Password.RequiredUniqueChars = 0;
                o.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddRoles<IdentityRole>()
            .AddDefaultTokenProviders();

            svcCollection.AddScoped<IdentitySeeder>();

            svcCollection.AddScoped<IUserClaimsPrincipalFactory<WarHackUser>, AppUserClaimsPrincipalFactory>();
            svcCollection.AddScoped<IAthenaeum, Athenaeum>();

            svcCollection.AddAuthorization(o =>
            {
                o.AddPolicy("IsAdmin", p => p.RequireRole("Admin"));
                o.AddPolicy("HasBattleTag", p => p.RequireClaim("battletag"));
            });

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

            //register clientel
            // svcCollection.AddHttpClient<IClient, CpiClientBase>("whClient", c =>
            // {
            //     c.BaseAddress = new Uri(cpiBaseUri);
            //     c.DefaultRequestHeaders.Add("Accept", "application/json");
            // }).AddPolicyHandler(RetryPolicy()).AddPolicyHandler(FallbackPolicy());
        }
    }
}
