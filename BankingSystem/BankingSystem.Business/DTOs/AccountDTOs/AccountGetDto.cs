using BankingSystem.Core.Enums;

namespace BankingSystem.Business.DTOs.AccountDTOs
{
    public record AccountGetDto(int Id, string AccountNumber, decimal Balance, Currency Currency, AccountStatus Status, 
                            string AppUserId, byte[] RowVersion);
}
