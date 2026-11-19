using BankingSystem.MVC.Enums;

namespace BankingSystem.MVC.ViewModels.Cards;

public class CardViewModel
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string CardNumberLast4 { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public CardStatus Status { get; set; }
    public decimal Balance { get; set; }
    public decimal DailyLimit { get; set; }

    public string? RevealedNumber { get; set; }

    public string MaskedNumber => $"•••• •••• •••• {CardNumberLast4}";
    public string ExpiryDisplay => ExpiryDate.ToString("MM/yy");

    public bool CanFreeze => Status == CardStatus.Active;
    public bool CanUnfreeze => Status == CardStatus.Frozen;
    public bool CanEdit => Status is CardStatus.Active or CardStatus.Frozen;
    public bool CanReveal => Status is CardStatus.Active or CardStatus.Frozen;
    public bool CanClose => Status is not (CardStatus.Closed or CardStatus.Expired);
}