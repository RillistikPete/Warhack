using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WarHack.Identity;

public class ApplicationDbContext : IdentityDbContext<WarHackUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)  : base(options)
    {
        
    }

}