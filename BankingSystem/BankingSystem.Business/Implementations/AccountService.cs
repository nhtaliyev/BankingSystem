using AutoMapper;
using BankingSystem.Business.DTOs.AccountDTOs;
using BankingSystem.Business.Exceptions;
using BankingSystem.Business.Interfaces;
using BankingSystem.Core.Enums;
using BankingSystem.Core.Models;
using BankingSystem.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace BankingSystem.Business.Implementations
{
    public class AccountService : IAccountService
    {
        private const int AccountNumberLength = 10;
        private const int MaxAccountNumberAttempts = 5;

        private readonly IAccountRepository _accountRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly TimeProvider _timeProvider;
        private readonly IMapper _mapper;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            IAccountRepository accountRepository,
            ICurrentUserService currentUser,
            TimeProvider timeProvider,
            IMapper mapper,
            ILogger<AccountService> logger)
        {
            _accountRepository = accountRepository;
            _currentUser = currentUser;
            _timeProvider = timeProvider;
            _mapper = mapper;
            _logger = logger;
        }

        private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;


        public async Task<AccountGetDto> CreateAccountAsync(AccountCreateDto dto, CancellationToken cancellationToken = default)
        {
            var ownerUserId = _currentUser.UserId;
            var now = UtcNow;

            var account = _mapper.Map<Account>(dto);
            account.AppUserId = ownerUserId;
            account.AccountNumber = await GenerateUniqueAccountNumberAsync(cancellationToken);
            account.Balance = 0m;
            account.Status = AccountStatus.Active;
            account.CreatedTime = now;
            account.UpdatedTime = now;

            await _accountRepository.AddAsync(account, cancellationToken);
            await _accountRepository.CommitAsync(cancellationToken); 

            _logger.LogInformation(
                "Account {AccountId} ({Currency}) opened for user {OwnerUserId}",
                account.Id, account.Currency, ownerUserId);

            return _mapper.Map<AccountGetDto>(account);
        }


        public async Task<AccountGetDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var account = await LoadAccountAsync(id, track: false, cancellationToken);
            return _mapper.Map<AccountGetDto>(account);
        }

        public async Task<ICollection<AccountGetDto>> GetByUserIdAsync(string appUserId, CancellationToken cancellationToken = default)
        {
            if (appUserId != _currentUser.UserId && !_currentUser.IsAdmin)
            {
                _logger.LogWarning(
                    "Access denied: user {UserId} tried to list the accounts of user {TargetUserId}",
                    _currentUser.UserId, appUserId);

                throw new ForbiddenException("You can only view your own accounts.");
            }

            var accounts = await _accountRepository
                .GetByExpression(asNoTracking: true, expression: a => a.AppUserId == appUserId).OrderBy(a => a.Id)
                .ToListAsync(cancellationToken);

            return _mapper.Map<ICollection<AccountGetDto>>(accounts);
        }


        public async Task FreezeAccountAsync(int id, CancellationToken cancellationToken = default)
        {
            var account = await LoadAccountAsync(id, track: true, cancellationToken);

            EnsureNotClosed(account);

            if (account.Status == AccountStatus.Blocked)
                throw new BusinessValidationException($"Account with id '{id}' is blocked and must be unblocked before it can be frozen.");

            if (account.Status == AccountStatus.Frozen)
                throw new ConflictException($"Account with id '{id}' is already frozen.");

            await ChangeStatusAsync(account, AccountStatus.Frozen, cancellationToken);
        }

        public async Task UnfreezeAccountAsync(int id, CancellationToken cancellationToken = default)
        {
            var account = await LoadAccountAsync(id, track: true, cancellationToken);

            EnsureNotClosed(account);

            if (account.Status != AccountStatus.Frozen)
                throw new BusinessValidationException($"Account with id '{id}' is not frozen (current status: {account.Status}).");

            await ChangeStatusAsync(account, AccountStatus.Active, cancellationToken);
        }

        public async Task BlockAccountAsync(int id, CancellationToken cancellationToken = default)
        {
            EnsureAdmin("block", id);

            var account = await LoadAccountAsync(id, track: true, cancellationToken);

            EnsureNotClosed(account);

            if (account.Status == AccountStatus.Blocked)
                throw new ConflictException($"Account with id '{id}' is already blocked.");

            await ChangeStatusAsync(account, AccountStatus.Blocked, cancellationToken);
        }

        public async Task UnblockAccountAsync(int id, CancellationToken cancellationToken = default)
        {
            EnsureAdmin("unblock", id);

            var account = await LoadAccountAsync(id, track: true, cancellationToken);

            EnsureNotClosed(account);

            if (account.Status != AccountStatus.Blocked)
                throw new BusinessValidationException($"Account with id '{id}' is not blocked (current status: {account.Status}).");

            await ChangeStatusAsync(account, AccountStatus.Active, cancellationToken);
        }

        public async Task CloseAccountAsync(int id, CancellationToken cancellationToken = default)
        {
            var account = await _accountRepository
                .GetByExpression(false, a => a.Id == id, a => a.Cards)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw AccountNotFound(id);

            EnsureCanAccess(account);

            if (account.Status == AccountStatus.Closed)
                throw new ConflictException($"Account with id '{id}' is already closed.");

            if (account.Balance != 0m)
                throw new BusinessValidationException("Account must have a zero balance before it can be closed.");

            if (account.Cards.Any(c => c.Status != CardStatus.Closed))
                throw new BusinessValidationException("All cards on this account must be closed before the account can be closed.");

            await ChangeStatusAsync(account, AccountStatus.Closed, cancellationToken);
        }

       
        private async Task<Account> LoadAccountAsync(int id, bool track, CancellationToken cancellationToken)
        {
            var account = await _accountRepository
                .GetByExpression(!track, a => a.Id == id)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw AccountNotFound(id);

            EnsureCanAccess(account);
            return account;
        }

        private void EnsureCanAccess(Account account)
        {
            if (_currentUser.IsAdmin || account.AppUserId == _currentUser.UserId)
                return;

            _logger.LogWarning(
                "Access denied: user {UserId} tried to access account {AccountId} that belongs to another user",
                _currentUser.UserId, account.Id);

            throw AccountNotFound(account.Id);
        }

        private void EnsureAdmin(string action, int accountId)
        {
            if (_currentUser.IsAdmin)
                return;

            _logger.LogWarning(
                "Access denied: user {UserId} tried to {Action} account {AccountId} without the Admin role",
                _currentUser.UserId, action, accountId);

            throw new ForbiddenException("This action requires the Admin role.");
        }

        private static void EnsureNotClosed(Account account)
        {
            if (account.Status == AccountStatus.Closed)
                throw new AccountClosedException($"Account with id '{account.Id}' is closed and cannot be modified.");
        }

        private async Task ChangeStatusAsync(Account account, AccountStatus newStatus, CancellationToken cancellationToken)
        {
            var oldStatus = account.Status;

            account.Status = newStatus;
            account.UpdatedTime = UtcNow;

            await SaveAsync(account.Id, cancellationToken);

            _logger.LogInformation(
                "Account {AccountId} status changed from {OldStatus} to {NewStatus} by user {ActorUserId}",
                account.Id, oldStatus, newStatus, _currentUser.UserId);
        }

        private async Task SaveAsync(int accountId, CancellationToken cancellationToken)
        {
            try
            {
                await _accountRepository.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict while saving account {AccountId}", accountId);
                throw new ConflictException($"Account with id '{accountId}' was modified by another request. Reload and try again.");
            }
        }

        private async Task<string> GenerateUniqueAccountNumberAsync(CancellationToken cancellationToken)
        {
            for (var attempt = 1; attempt <= MaxAccountNumberAttempts; attempt++)
            {
                var candidate = GenerateAccountNumber();

                var exists = await _accountRepository
                    .GetByExpression(asNoTracking: true, expression: a => a.AccountNumber == candidate)
                    .AnyAsync(cancellationToken);

                if (!exists)
                    return candidate;

                _logger.LogWarning("Generated account number collided with an existing account (attempt {Attempt})", attempt);
            }

            throw new InvalidOperationException("Failed to generate a unique account number after several attempts.");
        }

        private static string GenerateAccountNumber()
        {
            Span<char> digits = stackalloc char[AccountNumberLength];
            for (var i = 0; i < digits.Length; i++)
                digits[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));

            return new string(digits);
        }

        private static NotFoundException AccountNotFound(int id) =>
            new($"Account with id '{id}' was not found.");
    }
}