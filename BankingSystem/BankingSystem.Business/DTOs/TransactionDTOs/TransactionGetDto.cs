using BankingSystem.Core.Enums;

namespace BankingSystem.Business.DTOs.TransactionDTOs
{
    public record TransactionGetDto(
        int Id,
        int? FromAccountId,
        int? ToAccountId,
        int? FromCardId,
        int? ToCardId,
        decimal Amount,
        Currency Currency,
        TransactionType Type,
        TransactionStatus Status,
        string? Description,
        DateTime CreatedAt
    );
}
