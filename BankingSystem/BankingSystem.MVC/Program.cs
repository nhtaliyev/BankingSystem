using BankingSystem.MVC.Extensions;
using BankingSystem.MVC.Filters;
using BankingSystem.MVC.Options;
using BankingSystem.MVC.Services.ApiClients.Implementations;
using BankingSystem.MVC.Services.ApiClients.Interfaces;
using BankingSystem.MVC.Services.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace BankingSystem.MVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

                options.Filters.Add(new AuthorizeFilter());

                options.Filters.Add<ApiExceptionFilter>();
            });

            builder.Services.AddOptions<ApiOptions>()
                .BindConfiguration(ApiOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddTransient<TokenHandler>();

            builder.Services.AddHttpClient(TokenHandler.RawClientName, ApiClientRegistration.ConfigureClient);

            builder.Services.AddApiClient<IAuthApi, AuthApi>();
            builder.Services.AddApiClient<IAccountsApi, AccountsApi>();
            builder.Services.AddApiClient<ICardsApi, CardsApi>();

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "BankingSystem.Auth";
                    options.Cookie.HttpOnly = true;                           
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  
                    options.Cookie.SameSite = SameSiteMode.Strict;            
                    options.LoginPath = "/Auth/Login";
                    options.AccessDeniedPath = "/Auth/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);            
                    options.SlidingExpiration = false;
                });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            var enUs = new CultureInfo("en-US");
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(enUs),
                SupportedCultures = [enUs],
                SupportedUICultures = [enUs]
            });

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
