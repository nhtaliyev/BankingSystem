using BankingSystem.MVC.Services.ApiClients.Models;

namespace BankingSystem.MVC.Services.ApiClients.Interfaces;

public interface IAccountsApi
{
    Task<IReadOnlyList<AccountResponse>> GetMineAsync(CancellationToken cancellationToken = default);
    Task<AccountResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AccountResponse> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken = default);
    Task FreezeAsync(int id, CancellationToken cancellationToken = default);
    Task UnfreezeAsync(int id, CancellationToken cancellationToken = default);
    Task CloseAsync(int id, CancellationToken cancellationToken = default);
}