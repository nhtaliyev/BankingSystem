using BankingSystem.Business.ExternalServices.Implementations;
using BankingSystem.Business.ExternalServices.Interfaces;
using BankingSystem.Business.Implementations;
using BankingSystem.Business.Interfaces;
using BankingSystem.Business.Mappings;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BankingSystem.Business
{
    public static class ServiceRegistration
    {
        public static void AddServices(this IServiceCollection services)
        {
            // AutoMapper 15+ is under a commercial license (free tier available). If you get a license
            // warning in the logs, set cfg.LicenseKey here from configuration.
            services.AddAutoMapper(cfg => cfg.AddProfile<MapProfile>());

            services.AddValidatorsFromAssemblyContaining<MapProfile>();

            services.AddSingleton<ICardEncryptionService, CardEncryptionService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<ICardService, CardService>();

        }
    }
}
