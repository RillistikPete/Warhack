using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WarHack.Identity;
using WarHack.Logging;

namespace WarHack.Data;


public class WarHackDbContext : IdentityDbContext<WarHackUser>
{
    public WarHackDbContext(DbContextOptions<WarHackDbContext> options) : base(options)
    {
    }

    public DbSet<LogQueue> LogQueue => Set<LogQueue>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<LogQueue>(e =>
        {
            e.ToTable("LogQueue");
            e.HasIndex(x => x.DateAdded);
            e.HasIndex(x => x.Level);
        });
    }
}
