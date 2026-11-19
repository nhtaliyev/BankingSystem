using BankingSystem.MVC.Enums;
using BankingSystem.MVC.ViewModels.Cards;

namespace BankingSystem.MVC.ViewModels.Accounts;

public class AccountViewModel
{
    public int Id { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public Currency Currency { get; set; }
    public AccountStatus Status { get; set; }

    public List<CardViewModel> Cards { get; set; } = [];

    public bool CanFreeze => Status == AccountStatus.Active;
    public bool CanUnfreeze => Status == AccountStatus.Frozen;
    public bool CanClose => Status is AccountStatus.Active or AccountStatus.Frozen;
    public bool CanAddCard => Status == AccountStatus.Active;
}