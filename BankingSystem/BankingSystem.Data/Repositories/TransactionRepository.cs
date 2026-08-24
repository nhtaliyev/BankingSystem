using BankingSystem.Core.Models;
using BankingSystem.Core.Repositories;

namespace BankingSystem.Data.Repositories
{
    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(AppDbContext context) : base(context)
        {
        }
    }
}
