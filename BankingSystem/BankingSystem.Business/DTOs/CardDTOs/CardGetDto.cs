using BankingSystem.Core.Enums;

namespace BankingSystem.Business.DTOs.CardDTOs
{
    public record CardGetDto(int Id, string CardNumberLast4, DateTime ExpiryDate, CardStatus Status,
                        decimal Balance, decimal DailyLimit, int AccountId);
}
