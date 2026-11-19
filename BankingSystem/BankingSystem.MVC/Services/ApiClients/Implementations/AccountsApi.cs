using BankingSystem.MVC.Services.ApiClients.Interfaces;
using BankingSystem.MVC.Services.ApiClients.Models;

namespace BankingSystem.MVC.Services.ApiClients.Implementations;

public class AccountsApi : ApiClientBase, IAccountsApi
{
    public AccountsApi(HttpClient http) : base(http) { }

    public async Task<IReadOnlyList<AccountResponse>> GetMineAsync(CancellationToken cancellationToken = default) =>
        await SendAsync<List<AccountResponse>>(HttpMethod.Get, "api/accounts/my", cancellationToken: cancellationToken);

    public Task<AccountResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        SendAsync<AccountResponse>(HttpMethod.Get, $"api/accounts/{id}", cancellationToken: cancellationToken);

    public Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<AccountResponse>(HttpMethod.Post, "api/accounts", request, cancellationToken);

    public Task FreezeAsync(int id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/accounts/{id}/freeze", cancellationToken: cancellationToken);

    public Task UnfreezeAsync(int id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/accounts/{id}/unfreeze", cancellationToken: cancellationToken);

    public Task CloseAsync(int id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/accounts/{id}/close", cancellationToken: cancellationToken);
}