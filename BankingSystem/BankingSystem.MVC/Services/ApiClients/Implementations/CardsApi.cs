using BankingSystem.MVC.Services.ApiClients.Interfaces;
using BankingSystem.MVC.Services.ApiClients.Models;

namespace BankingSystem.MVC.Services.ApiClients.Implementations
{
    public class CardsApi : ApiClientBase, ICardsApi
    {
        public CardsApi(HttpClient http) : base(http) { }

        public Task<CardResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            SendAsync<CardResponse>(HttpMethod.Get, $"api/cards/{id}", cancellationToken: cancellationToken);

        public async Task<IReadOnlyList<CardResponse>> GetByAccountAsync(int accountId, CancellationToken cancellationToken = default) =>
            await SendAsync<List<CardResponse>>(HttpMethod.Get, $"api/cards/account/{accountId}", cancellationToken: cancellationToken);

        public Task<CardResponse> CreateAsync(CreateCardRequest request, CancellationToken cancellationToken = default) =>
            SendAsync<CardResponse>(HttpMethod.Post, "api/cards", request, cancellationToken);

        public Task UpdateAsync(int id, UpdateCardRequest request, CancellationToken cancellationToken = default) =>
            SendAsync(HttpMethod.Put, $"api/cards/{id}", request, cancellationToken);

        public Task FreezeAsync(int id, CancellationToken cancellationToken = default) =>
            SendAsync(HttpMethod.Post, $"api/cards/{id}/freeze", cancellationToken: cancellationToken);

        public Task UnfreezeAsync(int id, CancellationToken cancellationToken = default) =>
            SendAsync(HttpMethod.Post, $"api/cards/{id}/unfreeze", cancellationToken: cancellationToken);

        public Task CloseAsync(int id, CancellationToken cancellationToken = default) =>
            SendAsync(HttpMethod.Post, $"api/cards/{id}/close", cancellationToken: cancellationToken);

        public Task<RevealCardResponse> RevealAsync(int id, CancellationToken cancellationToken = default) =>
            SendAsync<RevealCardResponse>(HttpMethod.Post, $"api/cards/{id}/reveal", cancellationToken: cancellationToken);
    }
}
