using BankingSystem.Business.DTOs.TransactionDTOs;

namespace BankingSystem.Business.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionGetDto> TransferAsync(string appUserId, TransactionCreateDto dto, CancellationToken ct = default);
        Task<TransactionGetDto> GetByIdAsync(string appUserId, int transactionId, CancellationToken ct = default);
        Task<List<TransactionGetDto>> GetMyTransactionsAsync(string appUserId, CancellationToken ct = default);
    }
}
