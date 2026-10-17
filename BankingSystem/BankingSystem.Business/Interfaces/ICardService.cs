using BankingSystem.Business.DTOs.CardDTOs;

namespace BankingSystem.Business.Interfaces
{
    public interface ICardService
    {
        Task<CardGetDto> CreateCardAsync(CardCreateDto dto, CancellationToken cancellationToken = default);
        Task<CardGetDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ICollection<CardGetDto>> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default);
        Task UpdateCardAsync(int id, CardEditDto dto, CancellationToken cancellationToken = default);

        Task FreezeCardAsync(int id, CancellationToken cancellationToken = default);
        Task UnfreezeCardAsync(int id, CancellationToken cancellationToken = default);
        Task BlockCardAsync(int id, CancellationToken cancellationToken = default);
        Task UnblockCardAsync(int id, CancellationToken cancellationToken = default);
        Task CloseCardAsync(int id, CancellationToken cancellationToken = default);

        Task<CardNumberRevealDto> RevealCardNumberAsync(int id, CancellationToken cancellationToken = default);
    }
}