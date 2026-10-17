
using BankingSystem.Core.Models;
using BankingSystem.Data;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace BankingSystem.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
                .Build())
            .CreateBootstrapLogger();

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowAll",
                        builder =>
                        {
                            builder.AllowAnyOrigin()
                                   .AllowAnyMethod()
                                   .AllowAnyHeader();
                        });
                });

                // Add services to the container.

                builder.Services.AddControllers();

                builder.Services.AddOpenApi();

                builder.Services.AddIdentity<AppUser, IdentityRole>(opt =>
                {
                    opt.Password.RequiredLength = 8;
                    opt.Password.RequireNonAlphanumeric = true;
                    opt.Password.RequiredUniqueChars = 1;
                    opt.Password.RequireUppercase = true;
                    opt.Password.RequireLowercase = true;
                    opt.Password.RequireDigit = true;

                    opt.User.RequireUniqueEmail = true;
                }).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

                builder.Services.AddRepositories(builder.Configuration.GetConnectionString("DefaultConnection"));
                //builder.Services.AddServices();
                builder.Services.AddEndpointsApiExplorer();
                //builder.Services.AddSwaggerGen();

                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();
                    //app.UseSwagger();
                    //app.UseSwaggerUI();
                }

                app.UseCors("AllowAll");
                app.UseHttpsRedirection();
                app.UseAuthentication();
                app.UseAuthorization();


                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
