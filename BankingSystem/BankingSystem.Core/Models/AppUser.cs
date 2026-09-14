using Microsoft.AspNetCore.Identity;

namespace BankingSystem.Core.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; }
        public DateTime Birthday { get; set; }

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}
