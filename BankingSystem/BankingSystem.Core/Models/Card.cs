using BankingSystem.Core.Enums;

namespace BankingSystem.Core.Models
{
    public class Card : BaseModel
    {
        public string CardNumber { get; set; }
        public DateTime ExpiryDate { get; set; }

        public CardStatus Status { get; set; }

        public decimal Balance { get; set; }
        public decimal DailyLimit { get; set; }

        public int AccountId { get; set; }
        public Account Account { get; set; }

        public ICollection<Transaction> Transactions { get; set; }
    }
}
