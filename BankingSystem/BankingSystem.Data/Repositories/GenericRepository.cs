using BankingSystem.Core.Models;
using BankingSystem.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BankingSystem.Data.Repositories
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseModel, new()
    {
        private readonly AppDbContext _context;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
        }

        public DbSet<TEntity> Table => _context.Set<TEntity>();

        public async Task<int> CommitAsync()
            => await _context.SaveChangesAsync();

        public async Task CreateAsync(TEntity entity)
            => await Table.AddAsync(entity);

        public void Delete(TEntity entity)
            => Table.Remove(entity);

        public IQueryable<TEntity> GetByExpression(bool asnotracking = false, Expression<Func<TEntity, bool>>? expression = null, params string[] includes)
        {
            var query = Table.AsQueryable();
            if (includes.Length > 0)
            {
                foreach (var item in includes)
                {
                    query = query.Include(item);
                }
            }

            query = asnotracking == true ? query.AsNoTracking() : query;

            return expression is not null ? query.Where(expression) : query;
        }

        public async Task<TEntity> GetByIdAsync(int id)
            => await Table.FindAsync(id);
    }
}
