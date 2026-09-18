using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WarHack.Data;

public class WarHackDbContextFactory : IDesignTimeDbContextFactory<WarHackDbContext>
{
    public WarHackDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<WarHackDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = config.GetConnectionString("WarHackDb");

        if (string.IsNullOrWhiteSpace(cs))
        {
            throw new InvalidOperationException("Connection string 'WarHackDb' was empty. " + $"Base path: {Directory.GetCurrentDirectory()}");
        }

        var options = new DbContextOptionsBuilder<WarHackDbContext>().UseNpgsql(cs).Options;

        return new WarHackDbContext(options);
    }
}