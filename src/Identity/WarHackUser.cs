using Microsoft.AspNetCore.Identity;

namespace WarHack.Identity
{
    public class WarHackUser : IdentityUser
    {
        public string? BattleTag { get; set; }
        public string? UserImg { get; set; }
        public string? CharacterName { get; set; }
        public string? CharacterRealm { get; set; }
    }
}
