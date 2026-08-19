using BankingSystem.Core.Enums;

namespace BankingSystem.Core.Models
{
    public class Account : BaseModel
    {
        public string AccountNumber { get; set; }

        public Currency Currency { get; set; }
        public AccountStatus Status { get; set; }

        public string AppUserId { get; set; }
        public AppUser User { get; set; }

        public ICollection<Card> Cards { get; set; }
    }
}
