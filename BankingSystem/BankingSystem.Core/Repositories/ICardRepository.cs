using BankingSystem.Core.Models;

namespace BankingSystem.Core.Repositories
{
    public interface ICardRepository : IGenericRepository<Card>
    {
        void SetOriginalRowVersion(Card card, uint rowVersion);
    }
}
