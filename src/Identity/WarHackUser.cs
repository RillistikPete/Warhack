using Microsoft.AspNetCore.Identity;
using WarHack.Domain;

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
