using BankingSystem.MVC.ViewModels;

namespace BankingSystem.MVC.Services.ApiClients;

public class AuthApi : ApiClientBase, IAuthApi
{
    public AuthApi(HttpClient http) : base(http) { }

    public Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<TokenResponse>(HttpMethod.Post, "api/auth/login", request, cancellationToken);

    public Task RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/auth/register", request, cancellationToken);

    public Task LogoutAsync(CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/auth/logout", cancellationToken: cancellationToken);
}