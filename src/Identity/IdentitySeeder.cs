namespace WarHack.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static class WarHackRoles
{
    public const string Admin = "Admin";
    public const string Member = "Member";

    public static readonly string[] All = [Admin, Member];
}

public class IdentitySeeder
{
    private readonly RoleManager<IdentityRole> _roles;
    private readonly UserManager<WarHackUser> _users;
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        RoleManager<IdentityRole> roles,
        UserManager<WarHackUser> users,
        ApplicationDbContext db,
        IConfiguration config,
        ILogger<IdentitySeeder> logger)
    {
        _roles = roles;
        _users = users;
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await _db.Database.MigrateAsync(ct);
        await SeedRolesAsync();
        await SeedAdminAsync();
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in WarHackRoles.All)
        {
            if (await _roles.RoleExistsAsync(role)) continue;

            var result = await _roles.CreateAsync(new IdentityRole(role));
            if (result.Succeeded)
                _logger.LogInformation("Created role {Role}.", role);
            else
                _logger.LogError("Could not create role {Role}: {Errors}",
                    role, string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    private async Task SeedAdminAsync()
    {
        var section = _config.GetSection("SeedAdmin");
        var email = section["Email"];
        var userName = section["UserName"];
        var password = section["Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("No SeedAdmin configured — skipping admin seed.");
            return;
        }

        var admin = await _users.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new WarHackUser
            {
                UserName = userName ?? email,
                Email = email,
                EmailConfirmed = true
            };

            var created = await _users.CreateAsync(admin, password);
            if (!created.Succeeded)
            {
                _logger.LogError("Could not create seed admin: {Errors}",
                    string.Join("; ", created.Errors.Select(e => e.Description)));
                return;
            }

            _logger.LogInformation("Created seed admin {Email}.", email);
        }

        if (!await _users.IsInRoleAsync(admin, WarHackRoles.Admin))
        {
            await _users.AddToRoleAsync(admin, WarHackRoles.Admin);
            await _users.UpdateSecurityStampAsync(admin);
            _logger.LogInformation("Granted Admin to {Email}.", email);
        }
    }
}

public static class SeedExtensions
{
    public static async Task SeedIdentityAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync();
    }
}