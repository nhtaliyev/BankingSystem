using BankingSystem.Core.Models;
using BankingSystem.Core.Repositories;

namespace BankingSystem.Data.Repositories
{
    public class CardRepository : GenericRepository<Card>, ICardRepository
    {
        private readonly AppDbContext _context;

        public CardRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public Task SetOriginalRowVersionAsync(Card card, byte[] rowVersion, CancellationToken cancellationToken = default)
        {
            _context.Entry(card).Property(c => c.RowVersion).OriginalValue = rowVersion;
            return Task.CompletedTask;
        }
    }
}
