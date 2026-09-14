using BankingSystem.Core.Models;
using BankingSystem.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BankingSystem.Data.Repositories
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseModel
    {
        private readonly AppDbContext _context;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
        }

        public DbSet<TEntity> Table => _context.Set<TEntity>();

        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);

        public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
            => await Table.AddAsync(entity, cancellationToken);

        public void Delete(TEntity entity)
            => Table.Remove(entity);

        public IQueryable<TEntity> GetByExpression(bool asNoTracking = false, Expression<Func<TEntity, bool>>? expression = null,
            params Expression<Func<TEntity, object>>[] includes)
        {
            var query = Table.AsQueryable();

            foreach (var include in includes)
                query = query.Include(include);

            if (asNoTracking)
                query = query.AsNoTracking();

            return expression is not null ? query.Where(expression) : query;
        }

        public async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => await Table.FindAsync(id, cancellationToken);
    }
}