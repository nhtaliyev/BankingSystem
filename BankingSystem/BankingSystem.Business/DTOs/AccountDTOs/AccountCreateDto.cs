using BankingSystem.Core.Enums;

namespace BankingSystem.Business.DTOs.AccountDTOs
{
    public record AccountCreateDto(string AppUserId, Currency Currency);
}
