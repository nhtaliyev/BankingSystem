using BankingSystem.Core.Models;
using BankingSystem.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.API.Extensions
{
    public static class DatabaseInitializer
    {
        public const string AdminRole = "Admin";
        public const string CustomerRole = "Customer";
        
        public static async Task InitializeAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("BankingSystem.API.DatabaseInitializer");

            if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
            {
                logger.LogInformation("Applying pending database migrations");
                await sp.GetRequiredService<AppDbContext>().Database.MigrateAsync();
            }

            await SeedRolesAsync(sp, logger);
            await SeedAdminAsync(sp, app.Configuration, logger);
        }

        private static async Task SeedRolesAsync(IServiceProvider sp, ILogger logger)
        {
            var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var role in new[] { AdminRole, CustomerRole })
            {
                if (await roleManager.RoleExistsAsync(role))
                    continue;

                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                    throw new InvalidOperationException(
                        $"Could not create role '{role}': {string.Join("; ", result.Errors.Select(e => e.Description))}");

                logger.LogInformation("Created role {Role}", role);
            }
        }

        private static async Task SeedAdminAsync(IServiceProvider sp, IConfiguration config, ILogger logger)
        {
            var email = config["Seed:AdminEmail"];
            var password = config["Seed:AdminPassword"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogInformation("No admin seed configured (Seed:AdminEmail / Seed:AdminPassword); skipping");
                return;
            }

            var userManager = sp.GetRequiredService<UserManager<AppUser>>();

            if (await userManager.FindByEmailAsync(email) is not null)
                return;

            var admin = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = config["Seed:AdminFullName"] ?? "Administrator",
                Birthday = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            var create = await userManager.CreateAsync(admin, password);
            if (!create.Succeeded)
                throw new InvalidOperationException(
                    $"Could not create the seed admin: {string.Join("; ", create.Errors.Select(e => e.Description))}");

            var addRole = await userManager.AddToRoleAsync(admin, AdminRole);
            if (!addRole.Succeeded)
                throw new InvalidOperationException(
                    $"Could not assign the Admin role: {string.Join("; ", addRole.Errors.Select(e => e.Description))}");

            logger.LogWarning("Seed admin {UserId} created", admin.Id);
        }
    }
}
