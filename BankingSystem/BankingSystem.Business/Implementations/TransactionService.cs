using AutoMapper;
using BankingSystem.Business.DTOs.TransactionDTOs;
using BankingSystem.Business.Exceptions;
using BankingSystem.Core.Enums;
using BankingSystem.Core.Models;
using BankingSystem.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace BankingSystem.Business.Implementations;

public class TransactionService : ITransactionService
{
    private readonly IGenericRepository<Transaction> _transactions;
    private readonly IGenericRepository<Account> _accounts;
    private readonly IGenericRepository<Card> _cards;
    private readonly IMapper _mapper;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        IGenericRepository<Transaction> transactions,
        IGenericRepository<Account> accounts,
        IGenericRepository<Card> cards,
        IMapper mapper, ILogger<TransactionService> logger)
    {
        _transactions = transactions;
        _accounts = accounts;
        _cards = cards;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<TransactionGetDto> TransferAsync(string appUserId, TransactionCreateDto dto, CancellationToken ct = default)
    {
        // 1. Cheap checks first, no DB access
        if (dto.Amount <= 0)
            throw new BadRequestException("Amount must be greater than zero.");
        if ((dto.FromAccountId is null) == (dto.FromCardId is null))
            throw new BadRequestException("Specify exactly one source: an account or a card.");
        if ((dto.ToAccountId is null) == (dto.ToCardId is null))
            throw new BadRequestException("Specify exactly one destination: an account or a card.");
        if ((dto.FromAccountId.HasValue && dto.FromAccountId == dto.ToAccountId) ||
            (dto.FromCardId.HasValue && dto.FromCardId == dto.ToCardId))
            throw new InvalidTransactionException("Source and destination cannot be the same.");

        // 2. Source first; prove ownership BEFORE touching anything else
        var source = await LoadSideAsync(dto.FromAccountId, dto.FromCardId, "Source", ct);
        if (source.OwnerId != appUserId)
            throw new NotFoundException("Source account or card was not found.");

        var destination = await LoadSideAsync(dto.ToAccountId, dto.ToCardId, "Destination", ct);

        // 3. Domain rules
        EnsureUsable(source, "Source");
        EnsureUsable(destination, "Destination");

        if (dto.Currency != source.Currency)
            throw new CurrencyMismatchException("Currency does not match the source.");
        if (source.Currency != destination.Currency)
            throw new CurrencyMismatchException("Source and destination currencies differ.");

        if (source.Balance < dto.Amount)
            throw new InsufficientFundsException("Insufficient funds.");

        if (source.Card is not null)
            await EnsureDailyLimitAsync(source.Card, dto.Amount, ct);

        // 4. Apply
        source.Debit(dto.Amount);
        destination.Credit(dto.Amount);

        var transaction = new Transaction
        {
            FromAccountId = dto.FromAccountId,
            FromCardId = dto.FromCardId,
            ToAccountId = dto.ToAccountId,
            ToCardId = dto.ToCardId,
            Amount = dto.Amount,
            Currency = source.Currency,
            Type = TransactionType.Transfer,
            Status = TransactionStatus.Completed,
            Description = dto.Description
        };

        await _transactions.AddAsync(transaction, ct);

        try
        {
            await _transactions.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The balance changed while processing. Please retry.");
        }

        return _mapper.Map<TransactionGetDto>(transaction);
    }

    public async Task<TransactionGetDto> GetByIdAsync(string appUserId, int transactionId, CancellationToken ct = default)
    {
        var transaction = await _transactions
            .GetByExpression(asNoTracking: true, expression: OwnedBy(appUserId))
            .Where(t => t.Id == transactionId)
            .FirstOrDefaultAsync(ct);

        if (transaction is null)
            throw new NotFoundException("Transaction was not found.");

        return _mapper.Map<TransactionGetDto>(transaction);
    }

    public async Task<List<TransactionGetDto>> GetMyTransactionsAsync(string appUserId, CancellationToken ct = default)
    {
        var list = await _transactions
            .GetByExpression(asNoTracking: true, expression: OwnedBy(appUserId))
            .OrderByDescending(t => t.CreatedTime)
            .ToListAsync(ct);

        return _mapper.Map<List<TransactionGetDto>>(list);
    }

    private sealed class TransferSide
    {
        public Account? Account { get; init; }
        public Card? Card { get; init; }

        public string OwnerId => Account?.AppUserId ?? Card!.Account.AppUserId;
        public Currency Currency => Account?.Currency ?? Card!.Account.Currency;
        public decimal Balance => Account?.Balance ?? Card!.Balance;

        public void Debit(decimal amount) { if (Account is not null) Account.Balance -= amount; else Card!.Balance -= amount; }
        public void Credit(decimal amount) { if (Account is not null) Account.Balance += amount; else Card!.Balance += amount; }
    }

    private async Task<TransferSide> LoadSideAsync(int? accountId, int? cardId, string label, CancellationToken ct)
    {
        if (accountId.HasValue)
        {
            var account = await _accounts.GetByIdAsync(accountId.Value, ct)
                ?? throw new NotFoundException($"{label} account or card was not found.");
            return new TransferSide { Account = account };
        }

        var card = await _cards
            .GetByExpression(asNoTracking: false, expression: c => c.Id == cardId!.Value, c => c.Account)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"{label} account or card was not found.");
        return new TransferSide { Card = card };
    }

    // One method for both directions: same rules apply to sender and receiver
    private static void EnsureUsable(TransferSide side, string label)
    {
        var account = side.Account ?? side.Card!.Account;

        switch (account.Status)
        {
            case AccountStatus.Frozen: throw new AccountFrozenException($"{label} account is frozen.");
            case AccountStatus.Closed: throw new AccountClosedException($"{label} account is closed.");
            case AccountStatus.Blocked: throw new InvalidTransactionException($"{label} account is blocked.");
        }

        if (side.Card is { } card)
        {
            if (card.Status != CardStatus.Active)
                throw new InvalidTransactionException($"{label} card is {card.Status}.");
            if (card.ExpiryDate < DateTime.UtcNow)
                throw new InvalidTransactionException($"{label} card has expired.");
        }
    }

    private async Task EnsureDailyLimitAsync(Card card, decimal amount, CancellationToken ct)
    {
        var startOfDay = DateTime.UtcNow.Date;
        var spentToday = await _transactions
            .GetByExpression(asNoTracking: true,
                expression: t => t.FromCardId == card.Id
                              && t.Status == TransactionStatus.Completed
                              && t.CreatedTime >= startOfDay)
            .SumAsync(t => t.Amount, ct);

        if (spentToday + amount > card.DailyLimit)
            throw new DailyLimitExceededException("Daily card limit exceeded.");
    }

    // An Expression (not a normal method) so EF Core can translate it to SQL
    private static Expression<Func<Transaction, bool>> OwnedBy(string userId) =>
        t => (t.FromAccount != null && t.FromAccount.AppUserId == userId)
          || (t.ToAccount != null && t.ToAccount.AppUserId == userId)
          || (t.FromCard != null && t.FromCard.Account.AppUserId == userId)
          || (t.ToCard != null && t.ToCard.Account.AppUserId == userId);
}