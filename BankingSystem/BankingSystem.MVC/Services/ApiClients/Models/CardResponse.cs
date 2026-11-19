using BankingSystem.MVC.Enums;

namespace BankingSystem.MVC.Services.ApiClients.Models;

public record CreateCardRequest(int AccountId, decimal DailyLimit);

public record UpdateCardRequest(decimal DailyLimit, uint RowVersion);

public record CardResponse(
    int Id,
    string CardNumberLast4,
    DateTime ExpiryDate,
    CardStatus Status,
    decimal Balance,
    decimal DailyLimit,
    int AccountId,
    uint RowVersion);

public record RevealCardResponse(string CardNumber);