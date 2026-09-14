using BankingSystem.Core.Enums;
using System.Transactions;
using TransactionStatus = BankingSystem.Core.Enums.TransactionStatus;

namespace BankingSystem.Core.Models
{
    public class Transaction : BaseModel
    {
        public int? FromAccountId { get; set; }
        public int? ToAccountId { get; set; }
        public Account? FromAccount { get; set; }
        public Account? ToAccount { get; set; }

        public int? FromCardId { get; set; }
        public int? ToCardId { get; set; }
        public Card? FromCard { get; set; }
        public Card? ToCard { get; set; }

        public decimal Amount { get; set; }
        public Currency Currency { get; set; }

        public TransactionType Type { get; set; }
        public TransactionStatus Status { get; set; }

        public string? Description { get; set; }
    }
}
