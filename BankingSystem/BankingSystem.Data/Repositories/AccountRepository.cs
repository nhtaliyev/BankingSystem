using BankingSystem.Core.Models;
using BankingSystem.Core.Repositories;

namespace BankingSystem.Data.Repositories
{
    public class AccountRepository : GenericRepository<Account>, IAccountRepository
    {
        public AccountRepository(AppDbContext context) : base(context)
        {
        }
    }
}
