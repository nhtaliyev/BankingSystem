using BankingSystem.Core.Models;
using BankingSystem.Core.Repositories;

namespace BankingSystem.Data.Repositories
{
    public class CardRepository : GenericRepository<Card>, ICardRepository
    {
        public CardRepository(AppDbContext context) : base(context)
        {
        }
    }
}
