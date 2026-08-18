using System.ComponentModel.DataAnnotations;

namespace WarHack.Infrastructure.Models
{
    public class RegisterModel
    {
        [Required] public string UserName { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, StringLength(100, MinimumLength = 12), DataType(DataType.Password)]
        public string Password { get; set; } = "";
        [Compare(nameof(Password), ErrorMessage = "Passwords must match."), DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = "";
        public string? BattleTag { get; set; }
    }
}
