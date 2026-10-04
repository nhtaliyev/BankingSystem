using AutoMapper;
using BankingSystem.Business.DTOs.TransactionDTOs;
using BankingSystem.Business.Exceptions;
using BankingSystem.Core.Enums;
using BankingSystem.Core.Models;
using BankingSystem.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public class TransactionService : ITransactionService
{
    private readonly IGenericRepository<Transaction> _transactions;
    private readonly IGenericRepository<Account> _accounts;
    private readonly IGenericRepository<Card> _cards;
    private readonly IMapper _mapper;

    public TransactionService(
        IGenericRepository<Transaction> transactions,
        IGenericRepository<Account> accounts,
        IGenericRepository<Card> cards,
        IMapper mapper)
    {
        _transactions = transactions;
        _accounts = accounts;
        _cards = cards;
        _mapper = mapper;
    }

    public async Task<TransactionGetDto> TransferAsync(string appUserId, TransferRequestDto dto, CancellationToken ct = default)
    {
        if (dto.Amount <= 0)
            throw new BadRequestException("Amount must be greater than zero.");
        if ((dto.FromAccountId is null) == (dto.FromCardId is null))
            throw new BadRequestException("Specify exactly one source: an account or a card.");
        if ((dto.ToAccountId is null) == (dto.ToCardId is null))
            throw new BadRequestException("Specify exactly one destination: an account or a card.");
        if (dto.FromAccountId.HasValue && dto.FromAccountId == dto.ToAccountId ||
            dto.FromCardId.HasValue && dto.FromCardId == dto.ToCardId)
            throw new InvalidTransactionException("Source and destination cannot be the same.");

        var source = await LoadSideAsync(dto.FromAccountId, dto.FromCardId, ct);
        var destination = await LoadSideAsync(dto.ToAccountId, dto.ToCardId, ct);

        if (source.OwnerId != appUserId)
            throw new NotFoundException("Source account or card was not found.");

        EnsureCanSend(source);
        EnsureCanReceive(destination);

        if (source.Currency != destination.Currency)
            throw new CurrencyMismatchException("Source and destination currencies differ.");

        if (source.Balance < dto.Amount)
            throw new InsufficientFundsException("Insufficient funds.");

        if (source.Card is not null)
            await EnsureDailyLimitAsync(source.Card, dto.Amount, ct);

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
            .GetByExpression(
                asNoTracking: true,
                expression: t => t.Id == transactionId && OwnedBy(t, appUserId))
            .FirstOrDefaultAsync(ct);

        if (transaction is null)
            throw new NotFoundException("Transaction was not found.");

        return _mapper.Map<TransactionGetDto>(transaction);
    }

    public async Task<List<TransactionGetDto>> GetMyTransactionsAsync(string appUserId, CancellationToken ct = default)
    {
        var list = await _transactions
            .GetByExpression(asNoTracking: true, expression: t => OwnedBy(t, appUserId))
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

    private async Task<TransferSide> LoadSideAsync(int? accountId, int? cardId, CancellationToken ct)
    {
        if (accountId.HasValue)
        {
            var account = await _accounts.GetByIdAsync(accountId.Value, ct)
                ?? throw new NotFoundException($"Account {accountId} was not found.");
            return new TransferSide { Account = account };
        }

        var card = await _cards
            .GetByExpression(asNoTracking: false, expression: c => c.Id == cardId!.Value, c => c.Account)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Card {cardId} was not found.");
        return new TransferSide { Card = card };
    }

    private static void EnsureCanSend(TransferSide side)
    {
        if (side.Account is not null)
        {
            switch (side.Account.Status)
            {
                case AccountStatus.Frozen: throw new AccountFrozenException("Source account is frozen.");
                case AccountStatus.Closed: throw new AccountClosedException("Source account is closed.");
                case AccountStatus.Blocked: throw new InvalidTransactionException("Source account is blocked.");
            }
        }
        else
        {
            EnsureCardUsable(side.Card!, "Source");
            EnsureAccountUsable(side.Card!.Account, "Source");
        }
    }

    private static void EnsureCanReceive(TransferSide side)
    {
        if (side.Account is not null) EnsureAccountUsable(side.Account, "Destination");
        else { EnsureCardUsable(side.Card!, "Destination"); EnsureAccountUsable(side.Card!.Account, "Destination"); }
    }

    private static void EnsureAccountUsable(Account a, string label)
    {
        switch (a.Status)
        {
            case AccountStatus.Frozen: throw new AccountFrozenException($"{label} account is frozen.");
            case AccountStatus.Closed: throw new AccountClosedException($"{label} account is closed.");
            case AccountStatus.Blocked: throw new InvalidTransactionException($"{label} account is blocked.");
        }
    }

    private static void EnsureCardUsable(Card c, string label)
    {
        if (c.Status != CardStatus.Active)
            throw new InvalidTransactionException($"{label} card is {c.Status}.");
        if (c.ExpiryDate < DateTime.UtcNow) 
            throw new InvalidTransactionException($"{label} card has expired.");
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

    private static Expression<Func<Transaction, bool>> OwnedBy(string userId) =>
        t => (t.FromAccount != null && t.FromAccount.AppUserId == userId)
          || (t.ToAccount != null && t.ToAccount.AppUserId == userId)
          || (t.FromCard != null && t.FromCard.Account.AppUserId == userId)
          || (t.ToCard != null && t.ToCard.Account.AppUserId == userId);

    private static readonly Expression<Func<Transaction, string, bool>> _unused = null!; 

    private static bool OwnedBy(Transaction t, string userId) =>
        t.FromAccount!.AppUserId == userId || t.ToAccount!.AppUserId == userId ||
        t.FromCard!.Account.AppUserId == userId || t.ToCard!.Account.AppUserId == userId;
}