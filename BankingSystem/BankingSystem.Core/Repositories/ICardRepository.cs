using BankingSystem.Core.Models;

namespace BankingSystem.Core.Repositories
{
    public interface ICardRepository : IGenericRepository<Card>
    {
        Task SetOriginalRowVersionAsync(Card card, byte[] rowVersion, CancellationToken cancellationToken = default);
    }
}
