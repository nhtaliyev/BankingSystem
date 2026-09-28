using AutoMapper;
using BankingSystem.Business.DTOs.AccountDTOs;
using BankingSystem.Business.Exceptions;
using BankingSystem.Business.Interfaces;
using BankingSystem.Core.Enums;
using BankingSystem.Core.Models;
using BankingSystem.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace BankingSystem.Business.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IMapper _mapper;

        public AccountService(IAccountRepository accountRepository, IMapper mapper)
        {
            _accountRepository = accountRepository;
            _mapper = mapper;
        }

        public async Task<AccountGetDto> CreateAccountAsync(AccountCreateDto dto, CancellationToken cancellationToken = default)
        {
            var account = _mapper.Map<Account>(dto);
            account.AccountNumber = await GenerateUniqueAccountNumberAsync(cancellationToken);
            account.Balance = 0m;
            account.Status = AccountStatus.Active;

            await _accountRepository.AddAsync(account, cancellationToken);
            await _accountRepository.CommitAsync(cancellationToken);

            return _mapper.Map<AccountGetDto>(account);
        }

        public async Task<AccountGetDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var account = await _accountRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Account with id '{id}' was not found.");

            return _mapper.Map<AccountGetDto>(account);
        }

        public async Task<ICollection<AccountGetDto>> GetByUserIdAsync(string appUserId, CancellationToken cancellationToken = default)
        {
            var accounts = await _accountRepository
                .GetByExpression(asNoTracking: true, expression: a => a.AppUserId == appUserId)
                .ToListAsync(cancellationToken);

            return _mapper.Map<ICollection<AccountGetDto>>(accounts);
        }

        public async Task FreezeAccountAsync(int id, CancellationToken cancellationToken = default)
        {
            var account = await GetTrackedOrThrowAsync(id, cancellationToken);

            EnsureNotClosed(account);

            if (account.Status == AccountStatus.Blocked)
                throw new BusinessValidationException($"Account with id '{id}' is blocked and must be unblocked before it can be frozen.");

            if (account.Status == AccountStatus.Frozen)
                throw new ConflictException($"Account with id '{id}' is already frozen.");

            account.Status = AccountStatus.Frozen;
            await CommitAsync(id, cancellationToken);
        }

        public async Task UnfreezeAccountAsync(int id, CancellationToken cancellationToken = default)
        {
            var account = await GetTrackedOrThrowAsync(id, cancellationToken);

            EnsureNotClosed(account);

            if (account.Status != AccountStatus.Frozen)
                throw new BusinessValidationException($"Account with id '{id}' is not frozen (current status: {account.Status}).");

            account.Status = AccountStatus.Active;
            await CommitAsync(id, cancellationToken);
        }

        public async Task BlockAccountAsync(int id, CancellationToken cancellationToken = default)
        {
            var account = await GetTrackedOrThrowAsync(id, cancellationToken);

            EnsureNotClosed(account);

            if (account.Status == AccountStatus.Blocked)
                throw new ConflictException($"Account with id '{id}' is already blocked.");

            account.Status = AccountStatus.Blocked;
            await CommitAsync(id, cancellationToken);
        }

        public async Task UnblockAccountAsync(int id, CancellationToken cancellationToken = default)
        {
            var account = await GetTrackedOrThrowAsync(id, cancellationToken);

            EnsureNotClosed(account);

            if (account.Status != AccountStatus.Blocked)
                throw new BusinessValidationException($"Account with id '{id}' is not blocked (current status: {account.Status}).");

            account.Status = AccountStatus.Active;
            await CommitAsync(id, cancellationToken);
        }

        public async Task CloseAccountAsync(int id, CancellationToken cancellationToken = default)
        {
            var account = await _accountRepository
                .GetByExpression(asNoTracking: false, expression: a => a.Id == id, includes: a => a.Cards)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException($"Account with id '{id}' was not found.");

            if (account.Status == AccountStatus.Closed)
                throw new ConflictException($"Account with id '{id}' is already closed.");

            if (account.Balance != 0m)
                throw new BusinessValidationException("Account must have a zero balance before it can be closed.");

            if (account.Cards.Any(c => c.Status != CardStatus.Closed))
                throw new BusinessValidationException("All cards on this account must be closed before the account can be closed.");

            account.Status = AccountStatus.Closed;
            await CommitAsync(id, cancellationToken);
        }

        private async Task<Account> GetTrackedOrThrowAsync(int id, CancellationToken cancellationToken)
            => await _accountRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Account with id '{id}' was not found.");

        private static void EnsureNotClosed(Account account)
        {
            if (account.Status == AccountStatus.Closed)
                throw new AccountClosedException($"Account with id '{account.Id}' is closed and cannot be modified.");
        }

        private async Task CommitAsync(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _accountRepository.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException($"Account with id '{id}' was modified by another request. Reload and try again.");
            }
        }

        private async Task<string> GenerateUniqueAccountNumberAsync(CancellationToken cancellationToken)
        {
            const int maxAttempts = 5;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var candidate = GenerateAccountNumber();

                var exists = await _accountRepository
                    .GetByExpression(asNoTracking: true, expression: a => a.AccountNumber == candidate)
                    .AnyAsync(cancellationToken);

                if (!exists)
                    return candidate;
            }

            throw new ExternalServiceException("Failed to generate a unique account number after several attempts.");
        }

        private static string GenerateAccountNumber()
        {
            Span<byte> buffer = stackalloc byte[8];
            RandomNumberGenerator.Fill(buffer);
            var value = BitConverter.ToUInt64(buffer) % 10_000_000_000UL;
            return value.ToString("D10");
        }
    }
}