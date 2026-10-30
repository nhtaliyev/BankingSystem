using BankingSystem.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace BankingSystem.Core.Models
{
    public class Card : BaseModel
    {
        public string CardNumberLast4 { get; set; } = null!;
        public string CardNumberHash { get; set; } = null!;
        public string CardNumberEncrypted { get; set; } = null!;

        public DateTime ExpiryDate { get; set; }
        public CardStatus Status { get; set; }

        public decimal Balance { get; set; }
        public decimal DailyLimit { get; set; }

        public uint RowVersion { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; } = null!;

        public ICollection<Transaction> SentTransactions { get; set; } = new List<Transaction>();
        public ICollection<Transaction> ReceivedTransactions { get; set; } = new List<Transaction>();
    }
}
