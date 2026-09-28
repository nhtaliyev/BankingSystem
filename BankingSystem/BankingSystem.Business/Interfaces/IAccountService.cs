using BankingSystem.Business.DTOs.AccountDTOs;

namespace BankingSystem.Business.Interfaces
{
    public interface IAccountService
    {
        Task<AccountGetDto> CreateAccountAsync(AccountCreateDto dto, CancellationToken cancellationToken = default);
        Task<AccountGetDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ICollection<AccountGetDto>> GetByUserIdAsync(string appUserId, CancellationToken cancellationToken = default);

        Task FreezeAccountAsync(int id, CancellationToken cancellationToken = default);
        Task UnfreezeAccountAsync(int id, CancellationToken cancellationToken = default);
        Task BlockAccountAsync(int id, CancellationToken cancellationToken = default);
        Task UnblockAccountAsync(int id, CancellationToken cancellationToken = default);
        Task CloseAccountAsync(int id, CancellationToken cancellationToken = default);
    }
}