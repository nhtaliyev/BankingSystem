using BankingSystem.Core.Repositories;
using BankingSystem.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace BankingSystem.Data
{
    public static class ServiceRegistration
    {
        public static void AddRepositories(this IServiceCollection services, string connectionstring)
        {
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<ICardRepository, CardRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();

            services.AddDbContext<AppDbContext>(op =>
            {
                op.UseNpgsql(connectionstring);
            });
        }
    }
}
