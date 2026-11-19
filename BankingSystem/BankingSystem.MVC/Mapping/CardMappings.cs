using BankingSystem.MVC.Services.ApiClients.Models;
using BankingSystem.MVC.ViewModels.Cards;

namespace BankingSystem.MVC.Mapping
{
    public static class CardMappings
    {
        public static CardViewModel ToViewModel(this CardResponse card) => new()
        {
            Id = card.Id,
            AccountId = card.AccountId,
            CardNumberLast4 = card.CardNumberLast4,
            ExpiryDate = card.ExpiryDate,
            Status = card.Status,
            Balance = card.Balance,
            DailyLimit = card.DailyLimit
        };
    }
}
