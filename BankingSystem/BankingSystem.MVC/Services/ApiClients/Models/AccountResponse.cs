using BankingSystem.MVC.Enums;

namespace BankingSystem.MVC.Services.ApiClients.Models;

public record CreateAccountRequest(Currency Currency);

public record AccountResponse(
    int Id,
    string AccountNumber,
    decimal Balance,
    Currency Currency,
    AccountStatus Status,
    string AppUserId,
    uint RowVersion);