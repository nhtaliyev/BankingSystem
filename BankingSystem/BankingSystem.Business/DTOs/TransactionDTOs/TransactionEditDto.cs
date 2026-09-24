using BankingSystem.Core.Enums;

namespace BankingSystem.Business.DTOs.TransactionDTOs
{
    public record TransactionEditDto(
        TransactionStatus Status,
        string? Description
    );
}
