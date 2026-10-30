using BankingSystem.Core.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace BankingSystem.API.Extensions
{
    public static class JwtAuthenticationExtensions
    {
        private const string SecurityStampClaimType = "security_stamp";
        private const int MinSecretKeyBytes = 32;

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwt = configuration.GetSection("JWT");

            var secretKey = jwt["secretKey"]
                ?? throw new InvalidOperationException("JWT:secretKey is not configured.");

            if (string.IsNullOrWhiteSpace(jwt["issuer"]) || string.IsNullOrWhiteSpace(jwt["audience"]))
                throw new InvalidOperationException(
                    "JWT:issuer and JWT:audience must be configured (user-secrets or environment variables).");

            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            if (keyBytes.Length < MinSecretKeyBytes)
                throw new InvalidOperationException($"JWT:secretKey must be at least {MinSecretKeyBytes} bytes long.");

            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt["issuer"],
                        ValidateAudience = true,
                        ValidAudience = jwt["audience"],
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),
                        NameClaimType = ClaimTypes.Name,
                        RoleClaimType = ClaimTypes.Role
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("BankingSystem.API.JwtBearer");

                            if (context.Exception is SecurityTokenExpiredException)
                                logger.LogDebug("Rejected an expired access token");
                            else
                                logger.LogWarning("Rejected an invalid access token ({Reason})", context.Exception.GetType().Name);

                            return Task.CompletedTask;
                        },

                        OnTokenValidated = async context =>
                        {
                            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
                            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                            var tokenStamp = context.Principal?.FindFirstValue(SecurityStampClaimType);

                            var user = userId is null ? null : await userManager.FindByIdAsync(userId);

                            if (user is null
                                || tokenStamp is null
                                || user.SecurityStamp != tokenStamp
                                || await userManager.IsLockedOutAsync(user))
                            {
                                var logger = context.HttpContext.RequestServices
                                    .GetRequiredService<ILoggerFactory>()
                                    .CreateLogger("BankingSystem.API.JwtBearer");

                                logger.LogWarning(
                                    "Rejected a valid-looking token for user {UserId}: user missing, locked out, or security stamp changed",
                                    userId);

                                context.Fail("The token is no longer valid.");
                            }
                        }
                    };
                });

            return services;
        }
    }
}