using BankingSystem.MVC.Services.ApiClients.Models;

namespace BankingSystem.MVC.Services.ApiClients.Interfaces
{
    public interface ICardsApi
    {
        Task<CardResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CardResponse>> GetByAccountAsync(int accountId, CancellationToken cancellationToken = default);
        Task<CardResponse> CreateAsync(CreateCardRequest request, CancellationToken cancellationToken = default);
        Task UpdateAsync(int id, UpdateCardRequest request, CancellationToken cancellationToken = default);
        Task FreezeAsync(int id, CancellationToken cancellationToken = default);
        Task UnfreezeAsync(int id, CancellationToken cancellationToken = default);
        Task CloseAsync(int id, CancellationToken cancellationToken = default);
        Task<RevealCardResponse> RevealAsync(int id, CancellationToken cancellationToken = default);
    }
}
