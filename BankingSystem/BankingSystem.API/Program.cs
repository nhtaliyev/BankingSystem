using BankingSystem.API.Extensions;
using BankingSystem.API.Middlewares;
using BankingSystem.Business;
using BankingSystem.Business.ExternalServices;
using BankingSystem.Business.ExternalServices.Implementations;
using BankingSystem.Core.Models;
using BankingSystem.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace BankingSystem.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext());

                var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("Frontend", policy => policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod());
                });

                builder.Services.AddControllers();
                builder.Services.AddOpenApi();

                builder.Services.AddHttpContextAccessor();
                builder.Services.AddSingleton(TimeProvider.System);

                builder.Services.AddIdentity<AppUser, IdentityRole>(opt =>
                {
                    opt.Password.RequiredLength = 10;
                    opt.Password.RequireNonAlphanumeric = true;
                    opt.Password.RequiredUniqueChars = 4;
                    opt.Password.RequireUppercase = true;
                    opt.Password.RequireLowercase = true;
                    opt.Password.RequireDigit = true;

                    opt.User.RequireUniqueEmail = true;

                    opt.Lockout.AllowedForNewUsers = true;
                    opt.Lockout.MaxFailedAccessAttempts = 5;
                    opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                }).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

                builder.Services.AddJwtAuthentication(builder.Configuration);
                builder.Services.AddAuthorization();

                builder.Services.AddOptions<CardEncryptionOptions>()
                    .Bind(builder.Configuration.GetSection("CardEncryption"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                builder.Services.AddRateLimiter(options =>
                {
                    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                        RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                    options.AddPolicy("auth", context =>
                        RateLimitPartition.GetFixedWindowLimiter(ClientKey(context), _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 10,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                    options.OnRejected = (context, _) =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Rate limit exceeded for {Method} {Path} from {RemoteIp}",
                            context.HttpContext.Request.Method,
                            context.HttpContext.Request.Path,
                            context.HttpContext.Connection.RemoteIpAddress);

                        return ValueTask.CompletedTask;
                    };
                });

                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(connectionString))
                    throw new InvalidOperationException(
                        "ConnectionStrings:DefaultConnection is not configured. Use user-secrets (Development) or an environment variable (Production).");

                builder.Services.AddRepositories(connectionString);
                builder.Services.AddServices();

                var app = builder.Build();

                app.UseSerilogRequestLogging(options =>
                {
                    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                    {
                        diagnosticContext.Set("UserId",
                            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous");
                    };
                });

                if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();
                }
                else
                {
                    app.UseHsts();
                }

                app.UseHttpsRedirection();
                app.UseCors("Frontend");
                app.UseRateLimiter();

                app.UseAuthentication();
                app.UseMiddleware<LogUserContextMiddleware>();
                app.UseMiddleware<ExceptionHandlingMiddleware>();
                app.UseAuthorization();

                app.MapControllers();

                app.Run();
            }
            catch (Exception ex) when (ex is not HostAbortedException)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static string ClientKey(HttpContext context) =>
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}