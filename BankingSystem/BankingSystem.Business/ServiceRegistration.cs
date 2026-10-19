using BankingSystem.API.Services;
using BankingSystem.Business.Implementations;
using BankingSystem.Business.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BankingSystem.Business
{
    public static class ServiceRegistration
    {
        public static void AddServices(this IServiceCollection services)
        {
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<ICardService, CardService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
        }
    }
}
