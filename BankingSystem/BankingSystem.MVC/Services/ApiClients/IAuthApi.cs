using BankingSystem.MVC.ViewModels;

namespace BankingSystem.MVC.Services.ApiClients;

public interface IAuthApi
{
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
}