using AutoMapper;
using BankingSystem.Business.DTOs.TransactionDTOs;
using BankingSystem.Business.Exceptions;
using BankingSystem.Business.Interfaces;
using BankingSystem.Core.Enums;
using BankingSystem.Core.Models;
using BankingSystem.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace BankingSystem.Business.Implementations
{
    public class TransactionService : ITransactionService
    {
        private const int MoneyDecimals = 2;
        private const int MaxHistoryItems = 500;

        private readonly IGenericRepository<Transaction> _transactionRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly IGenericRepository<Card> _cardRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly TimeProvider _timeProvider;
        private readonly IMapper _mapper;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(
            IGenericRepository<Transaction> transactionRepository,
            IGenericRepository<Account> accountRepository,
            IGenericRepository<Card> cardRepository,
            ICurrentUserService currentUser,
            TimeProvider timeProvider,
            IMapper mapper,
            ILogger<TransactionService> logger)
        {
            _transactionRepository = transactionRepository;
            _accountRepository = accountRepository;
            _cardRepository = cardRepository;
            _currentUser = currentUser;
            _timeProvider = timeProvider;
            _mapper = mapper;
            _logger = logger;
        }

        private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

        public async Task<TransactionGetDto> TransferAsync(string appUserId, TransactionCreateDto dto, CancellationToken cancellationToken = default)
        {
            EnsureCaller(appUserId);

            if (dto.Amount <= 0)
                throw new BadRequestException("Amount must be greater than zero.");
            if (decimal.Round(dto.Amount, MoneyDecimals) != dto.Amount)
                throw new BadRequestException($"Amount can have at most {MoneyDecimals} decimal places.");
            if ((dto.FromAccountId is null) == (dto.FromCardId is null))
                throw new BadRequestException("Specify exactly one source: an account or a card.");
            if ((dto.ToAccountId is null) == (dto.ToCardId is null))
                throw new BadRequestException("Specify exactly one destination: an account or a card.");
            if ((dto.FromAccountId.HasValue && dto.FromAccountId == dto.ToAccountId) ||
                (dto.FromCardId.HasValue && dto.FromCardId == dto.ToCardId))
                throw new InvalidTransactionException("Source and destination cannot be the same.");

            var source = await LoadSideAsync(dto.FromAccountId, dto.FromCardId, "Source", cancellationToken);
            if (source.OwnerId != appUserId)
            {
                _logger.LogWarning(
                    "Access denied: user {UserId} tried to transfer from account {FromAccountId} / card {FromCardId} that belongs to another user",
                    appUserId, dto.FromAccountId, dto.FromCardId);

                throw new NotFoundException("Source account or card was not found.");
            }

            var destination = await LoadSideAsync(dto.ToAccountId, dto.ToCardId, "Destination", cancellationToken);

            var now = UtcNow;

            var sourceProblem = FindProblem(source, "Source", now);
            if (sourceProblem is not null)
            {
                _logger.LogWarning("Transfer rejected for user {UserId}: {Reason}", appUserId, sourceProblem.Message);
                throw sourceProblem;
            }

            var destinationProblem = FindProblem(destination, "Destination", now);
            if (destinationProblem is not null)
            {
                _logger.LogWarning("Transfer rejected for user {UserId}: {Reason}", appUserId, destinationProblem.Message);
                throw new InvalidTransactionException("The destination cannot receive this transfer.");
            }

            if (dto.Currency != source.Currency)
                throw new CurrencyMismatchException("Currency does not match the source.");
            if (source.Currency != destination.Currency)
                throw new CurrencyMismatchException("Source and destination currencies differ.");

            if (source.Balance < dto.Amount)
            {
                _logger.LogWarning(
                    "Transfer rejected for user {UserId}: insufficient funds for {Amount} {Currency} from account {FromAccountId} / card {FromCardId}",
                    appUserId, dto.Amount, source.Currency, dto.FromAccountId, dto.FromCardId);

                throw new InsufficientFundsException("Insufficient funds.");
            }

            if (source.Card is not null)
                await EnsureDailyLimitAsync(appUserId, source.Card, dto.Amount, now, cancellationToken);

            source.Debit(dto.Amount, now);
            destination.Credit(dto.Amount, now);

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
                Description = dto.Description,
                CreatedTime = now,
                UpdatedTime = now
            };

            await _transactionRepository.AddAsync(transaction, cancellationToken);

            try
            {
                await _transactionRepository.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning(
                    "Concurrency conflict while transferring {Amount} {Currency} for user {UserId} (from account {FromAccountId} / card {FromCardId}, to account {ToAccountId} / card {ToCardId})",
                    dto.Amount, source.Currency, appUserId, dto.FromAccountId, dto.FromCardId, dto.ToAccountId, dto.ToCardId);

                throw new ConflictException("The balance changed while processing. Please retry.");
            }

            _logger.LogInformation(
                "Transfer {TransactionId} completed: {Amount} {Currency} from account {FromAccountId} / card {FromCardId} to account {ToAccountId} / card {ToCardId} by user {UserId}",
                transaction.Id, transaction.Amount, transaction.Currency,
                dto.FromAccountId, dto.FromCardId, dto.ToAccountId, dto.ToCardId, appUserId);

            return _mapper.Map<TransactionGetDto>(transaction);
        }

        public async Task<TransactionGetDto> GetByIdAsync(string appUserId, int transactionId, CancellationToken cancellationToken = default)
        {
            EnsureCaller(appUserId);

            var transaction = await _transactionRepository
                .GetByExpression(asNoTracking: true, expression: OwnedBy(appUserId))
                .Where(t => t.Id == transactionId)
                .FirstOrDefaultAsync(cancellationToken);

            if (transaction is null)
                throw new NotFoundException("Transaction was not found.");

            return _mapper.Map<TransactionGetDto>(transaction);
        }

        public async Task<List<TransactionGetDto>> GetMyTransactionsAsync(string appUserId, CancellationToken cancellationToken = default)
        {
            EnsureCaller(appUserId);

            // Hard cap so one request can't pull an entire history. Real paging (like GetAllUsersAsync) is the proper fix.
            var list = await _transactionRepository
                .GetByExpression(asNoTracking: true, expression: OwnedBy(appUserId))
                .OrderByDescending(t => t.CreatedTime)
                .Take(MaxHistoryItems)
                .ToListAsync(cancellationToken);

            return _mapper.Map<List<TransactionGetDto>>(list);
        }

        // ───────────────────────────── Helpers ─────────────────────────────

        private sealed class TransferSide
        {
            public Account? Account { get; init; }
            public Card? Card { get; init; }

            public string OwnerId => Account?.AppUserId ?? Card!.Account.AppUserId;
            public Currency Currency => Account?.Currency ?? Card!.Account.Currency;
            public decimal Balance => Account?.Balance ?? Card!.Balance;

            public void Debit(decimal amount, DateTime now)
            {
                if (Account is not null) { Account.Balance -= amount; Account.UpdatedTime = now; }
                else { Card!.Balance -= amount; Card.UpdatedTime = now; }
            }

            public void Credit(decimal amount, DateTime now)
            {
                if (Account is not null) { Account.Balance += amount; Account.UpdatedTime = now; }
                else { Card!.Balance += amount; Card.UpdatedTime = now; }
            }
        }

        private async Task<TransferSide> LoadSideAsync(int? accountId, int? cardId, string label, CancellationToken cancellationToken)
        {
            if (accountId.HasValue)
            {
                var account = await _accountRepository.GetByIdAsync(accountId.Value, cancellationToken)
                    ?? throw new NotFoundException($"{label} account or card was not found.");
                return new TransferSide { Account = account };
            }

            var card = await _cardRepository
                .GetByExpression(false, c => c.Id == cardId!.Value, c => c.Account)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException($"{label} account or card was not found.");

            return new TransferSide { Card = card };
        }

        private static Exception? FindProblem(TransferSide side, string label, DateTime now)
        {
            var account = side.Account ?? side.Card!.Account;

            switch (account.Status)
            {
                case AccountStatus.Frozen: return new AccountFrozenException($"{label} account is frozen.");
                case AccountStatus.Closed: return new AccountClosedException($"{label} account is closed.");
                case AccountStatus.Blocked: return new InvalidTransactionException($"{label} account is blocked.");
            }

            if (side.Card is { } card)
            {
                if (card.Status != CardStatus.Active)
                    return new InvalidTransactionException($"{label} card is {card.Status}.");
                if (card.ExpiryDate <= now)
                    return new InvalidTransactionException($"{label} card has expired.");
            }

            return null;
        }

        private async Task EnsureDailyLimitAsync(string appUserId, Card card, decimal amount, DateTime now, CancellationToken cancellationToken)
        {
            var startOfDay = now.Date; // UTC midnight; see the note about the bank's local day
            var spentToday = await _transactionRepository
                .GetByExpression(asNoTracking: true,
                    expression: t => t.FromCardId == card.Id
                                  && t.Status == TransactionStatus.Completed
                                  && t.CreatedTime >= startOfDay)
                .SumAsync(t => t.Amount, cancellationToken);

            if (spentToday + amount > card.DailyLimit)
            {
                _logger.LogWarning(
                    "Transfer rejected for user {UserId}: daily limit of card {CardId} would be exceeded by {Amount}",
                    appUserId, card.Id, amount);

                throw new DailyLimitExceededException("Daily card limit exceeded.");
            }
        }

        private void EnsureCaller(string appUserId)
        {
            if (appUserId == _currentUser.UserId)
                return;

            _logger.LogWarning(
                "Access denied: user {UserId} called a transaction operation on behalf of user {TargetUserId}",
                _currentUser.UserId, appUserId);

            throw new ForbiddenException("You can only perform this action for your own account.");
        }

        private static Expression<Func<Transaction, bool>> OwnedBy(string userId) =>
            t => (t.FromAccount != null && t.FromAccount.AppUserId == userId)
              || (t.ToAccount != null && t.ToAccount.AppUserId == userId)
              || (t.FromCard != null && t.FromCard.Account.AppUserId == userId)
              || (t.ToCard != null && t.ToCard.Account.AppUserId == userId);
    }
}