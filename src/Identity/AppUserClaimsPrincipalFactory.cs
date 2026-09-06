using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using WarHack.Domain;

namespace WarHack.Identity;


public class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<WarHackUser, IdentityRole>
{
    public AppUserClaimsPrincipalFactory(
        UserManager<WarHackUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(WarHackUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        //validation and null checking
        if (!string.IsNullOrWhiteSpace(user.BattleTag))
            identity.AddClaim(new Claim("battletag", user.BattleTag));

        if (!string.IsNullOrWhiteSpace(user.CharacterName))
        {
            identity.AddClaim(new Claim("character:name", user.CharacterName));
            identity.AddClaim(new Claim("character:realm", user.CharacterRealm ?? ""));
        }

        return identity;
    }
}