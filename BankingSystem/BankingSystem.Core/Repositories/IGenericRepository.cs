using BankingSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BankingSystem.Core.Repositories
{
    public interface IGenericRepository<TEntity> where TEntity : BaseModel
    {
        public DbSet<TEntity> Table { get; }

        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

        void Delete(TEntity entity);

        IQueryable<TEntity> GetByExpression(bool asNoTracking = false, Expression<Func<TEntity, bool>>? expression = null,
            params Expression<Func<TEntity, object>>[] includes);

        Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<int> CommitAsync(CancellationToken cancellationToken = default);
    }
}