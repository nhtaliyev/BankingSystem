using BankingSystem.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace BankingSystem.Core.Models
{
    public class Account : BaseModel
    {
        public string AccountNumber { get; set; } = null!;
        public decimal Balance { get; set; }

        public Currency Currency { get; set; }
        public AccountStatus Status { get; set; }

        public uint RowVersion { get; set; }

        public string AppUserId { get; set; } = null!;
        public AppUser User { get; set; } = null!;

        public ICollection<Card> Cards { get; set; } = new List<Card>();
    }
}
