using BankingSystem.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BankingSystem.Core.Repositories
{
    public interface IGenericRepository<TEntity> where TEntity : BaseModel, new()
    {
        public DbSet<TEntity> Table { get; }
        Task CreateAsync(TEntity entity);
        void Delete(TEntity entity);
        IQueryable<TEntity> GetByExpression(bool asnotracking = false, Expression<Func<TEntity, bool>>? expression = null, params string[] includes);
        Task<TEntity> GetByIdAsync(int id);
        Task<int> CommitAsync();
    }
}
