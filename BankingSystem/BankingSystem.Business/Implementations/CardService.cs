using AutoMapper;
using BankingSystem.Business.DTOs.CardDTOs;
using BankingSystem.Business.Exceptions;
using BankingSystem.Business.ExternalServices.Interfaces;
using BankingSystem.Business.Interfaces;
using BankingSystem.Core.Enums;
using BankingSystem.Core.Models;
using BankingSystem.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace BankingSystem.Business.Implementations
{
    public class CardService : ICardService
    {
        private const int MaxCardNumberAttempts = 5;

        private readonly ICardRepository _cardRepository;
        private readonly IGenericRepository<Core.Models.Account> _accountRepository;
        private readonly ICardEncryptionService _cardEncryptionService;
        private readonly ICurrentUserService _currentUser;
        private readonly TimeProvider _timeProvider;
        private readonly IMapper _mapper;
        private readonly ILogger<CardService> _logger;

        public CardService(
            ICardRepository cardRepository,
            IGenericRepository<Core.Models.Account> accountRepository,
            ICardEncryptionService cardEncryptionService,
            ICurrentUserService currentUser,
            TimeProvider timeProvider,
            IMapper mapper,
            ILogger<CardService> logger)
        {
            _cardRepository = cardRepository;
            _accountRepository = accountRepository;
            _cardEncryptionService = cardEncryptionService;
            _currentUser = currentUser;
            _timeProvider = timeProvider;
            _mapper = mapper;
            _logger = logger;
        }

        private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

        // ───────────────────────────── Create ─────────────────────────────

        public async Task<CardGetDto> CreateCardAsync(CardCreateDto dto, CancellationToken cancellationToken = default)
        {
            var account = await _accountRepository.GetByIdAsync(dto.AccountId, cancellationToken)
                ?? throw AccountNotFound(dto.AccountId);

            EnsureCanAccessAccount(account);
            EnsureAccountIsActive(account);

            var (last4, hash, encrypted) = await GenerateUniqueCardNumberAsync(cancellationToken);
            var now = UtcNow;

            var card = _mapper.Map<Card>(dto);
            card.AccountId = account.Id;
            card.CardNumberLast4 = last4;
            card.CardNumberHash = hash;
            card.CardNumberEncrypted = encrypted;
            card.ExpiryDate = now.AddYears(4);
            card.Status = CardStatus.Active;
            card.Balance = 0m;
            card.CreatedTime = now;
            card.UpdatedTime = now;

            await _cardRepository.AddAsync(card, cancellationToken);
            await _cardRepository.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Card {CardId} (ending {Last4}) created for account {AccountId} with daily limit {DailyLimit}",
                card.Id, card.CardNumberLast4, card.AccountId, card.DailyLimit);

            return _mapper.Map<CardGetDto>(card);
        }

        // ────────────────────────────── Read ──────────────────────────────

        public async Task<CardGetDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var card = await LoadCardAsync(id, track: false, allowAdmin: true, cancellationToken);
            return _mapper.Map<CardGetDto>(card);
        }

        public async Task<ICollection<CardGetDto>> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
        {
            var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken)
                ?? throw AccountNotFound(accountId);

            EnsureCanAccessAccount(account);

            var cards = await _cardRepository
                .GetByExpression(asNoTracking: true, expression: c => c.AccountId == accountId)
                .ToListAsync(cancellationToken);

            return _mapper.Map<ICollection<CardGetDto>>(cards);
        }

        // ───────────────────────────── Update ─────────────────────────────

        public async Task UpdateCardAsync(int id, CardEditDto dto, CancellationToken cancellationToken = default)
        {
            var card = await LoadCardAsync(id, track: true, allowAdmin: true, cancellationToken);

            EnsureNotClosed(card);

            if (IsExpired(card))
                throw new BusinessValidationException($"Card with id '{id}' is expired and cannot be modified.");

            var oldLimit = card.DailyLimit;

            card.DailyLimit = dto.DailyLimit;
            card.UpdatedTime = UtcNow;

            _cardRepository.SetOriginalRowVersion(card, dto.RowVersion);
            await SaveAsync(id, cancellationToken);

            _logger.LogInformation(
                "Card {CardId} daily limit changed from {OldLimit} to {NewLimit}",
                id, oldLimit, card.DailyLimit);
        }

        // ──────────────────────── Status transitions ────────────────────────

        public async Task FreezeCardAsync(int id, CancellationToken cancellationToken = default)
        {
            var card = await LoadCardAsync(id, track: true, allowAdmin: true, cancellationToken);

            EnsureNotClosed(card);

            if (card.Status == CardStatus.Blocked)
                throw new BusinessValidationException($"Card with id '{id}' is blocked and must be unblocked before it can be frozen.");

            if (IsExpired(card))
                throw new BusinessValidationException($"Card with id '{id}' is expired and cannot be frozen.");

            if (card.Status == CardStatus.Frozen)
                throw new ConflictException($"Card with id '{id}' is already frozen.");

            await ChangeStatusAsync(card, CardStatus.Frozen, cancellationToken);
        }

        public async Task UnfreezeCardAsync(int id, CancellationToken cancellationToken = default)
        {
            var card = await LoadCardAsync(id, track: true, allowAdmin: true, cancellationToken);

            EnsureNotClosed(card);

            if (card.Status != CardStatus.Frozen)
                throw new BusinessValidationException($"Card with id '{id}' is not frozen (current status: {card.Status}).");

            if (IsExpired(card))
                throw new BusinessValidationException($"Card with id '{id}' is expired and cannot be unfrozen.");

            await ChangeStatusAsync(card, CardStatus.Active, cancellationToken);
        }

        public async Task BlockCardAsync(int id, CancellationToken cancellationToken = default)
        {
            EnsureAdmin("block", id);

            var card = await LoadCardAsync(id, track: true, allowAdmin: true, cancellationToken);

            EnsureNotClosed(card);

            if (IsExpired(card))
                throw new BusinessValidationException($"Card with id '{id}' is expired and cannot be blocked.");

            if (card.Status == CardStatus.Blocked)
                throw new ConflictException($"Card with id '{id}' is already blocked.");

            await ChangeStatusAsync(card, CardStatus.Blocked, cancellationToken);
        }

        public async Task UnblockCardAsync(int id, CancellationToken cancellationToken = default)
        {
            EnsureAdmin("unblock", id);

            var card = await LoadCardAsync(id, track: true, allowAdmin: true, cancellationToken);

            EnsureNotClosed(card);

            if (card.Status != CardStatus.Blocked)
                throw new BusinessValidationException($"Card with id '{id}' is not blocked (current status: {card.Status}).");

            if (IsExpired(card))
                throw new BusinessValidationException($"Card with id '{id}' is expired and cannot be unblocked.");

            await ChangeStatusAsync(card, CardStatus.Active, cancellationToken);
        }

        public async Task CloseCardAsync(int id, CancellationToken cancellationToken = default)
        {
            var card = await LoadCardAsync(id, track: true, allowAdmin: true, cancellationToken);

            if (card.Status == CardStatus.Closed)
                throw new ConflictException($"Card with id '{id}' is already closed.");

            if (card.Balance != 0m)
                throw new BusinessValidationException("Card must have a zero balance before it can be closed.");

            await ChangeStatusAsync(card, CardStatus.Closed, cancellationToken);
        }

        // ───────────────────────── Reveal card number ─────────────────────────

        public async Task<CardNumberRevealDto> RevealCardNumberAsync(int id, CancellationToken cancellationToken = default)
        {
            var card = await LoadCardAsync(id, track: false, allowAdmin: false, cancellationToken);

            if (card.Status is CardStatus.Closed or CardStatus.Blocked || IsExpired(card))
                throw new BusinessValidationException("The card number cannot be revealed because the card is closed, blocked or expired.");

            string cardNumber;
            try
            {
                cardNumber = _cardEncryptionService.Decrypt(card.CardNumberEncrypted);
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex,
                    "Failed to decrypt card number for card {CardId}. Possible data corruption, tampering, or wrong encryption key",
                    id);

                throw new ExternalServiceException("The card number could not be retrieved.");
            }

            _logger.LogInformation(
                "Full card number revealed for card {CardId} by user {UserId}",
                id, _currentUser.UserId);

            return new CardNumberRevealDto(cardNumber);
        }

        // ───────────────────────────── Helpers ─────────────────────────────

        private async Task<Card> LoadCardAsync(int id, bool track, bool allowAdmin, CancellationToken cancellationToken)
        {
            var card = await _cardRepository
                .GetByExpression(!track, c => c.Id == id, c => c.Account)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw CardNotFound(id);

            if (!CanAccess(card.Account, allowAdmin))
            {
                _logger.LogWarning(
                    "Access denied: user {UserId} tried to access card {CardId} that belongs to another user",
                    _currentUser.UserId, id);

                throw CardNotFound(id);
            }

            return card;
        }

        private void EnsureCanAccessAccount(Core.Models.Account account)
        {
            if (CanAccess(account, allowAdmin: true))
                return;

            _logger.LogWarning(
                "Access denied: user {UserId} tried to access account {AccountId} that belongs to another user",
                _currentUser.UserId, account.Id);

            throw AccountNotFound(account.Id);
        }

        private bool CanAccess(Core.Models.Account account, bool allowAdmin)
        {
            return (allowAdmin && _currentUser.IsAdmin)
                   || account.AppUserId == _currentUser.UserId;
        }

        private void EnsureAdmin(string action, int cardId)
        {
            if (_currentUser.IsAdmin)
                return;

            _logger.LogWarning(
                "Access denied: user {UserId} tried to {Action} card {CardId} without the Admin role",
                _currentUser.UserId, action, cardId);

            throw new ForbiddenException("This action requires the Admin role.");
        }

        private static void EnsureAccountIsActive(Core.Models.Account account)
        {
            if (account.Status != AccountStatus.Active)
                throw new BusinessValidationException(
                    $"Cards can only be issued for active accounts (account '{account.Id}' status: {account.Status}).");
        }

        private static void EnsureNotClosed(Card card)
        {
            if (card.Status == CardStatus.Closed)
                throw new BusinessValidationException($"Card with id '{card.Id}' is closed and cannot be modified.");
        }

        private bool IsExpired(Card card)
        {
            return card.Status == CardStatus.Expired || card.ExpiryDate <= UtcNow;
        }

        private async Task ChangeStatusAsync(Card card, CardStatus newStatus, CancellationToken cancellationToken)
        {
            var oldStatus = card.Status;

            card.Status = newStatus;
            card.UpdatedTime = UtcNow;

            await SaveAsync(card.Id, cancellationToken);

            _logger.LogInformation(
                "Card {CardId} status changed from {OldStatus} to {NewStatus}",
                card.Id, oldStatus, newStatus);
        }

        private async Task SaveAsync(int cardId, CancellationToken cancellationToken)
        {
            try
            {
                await _cardRepository.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                _logger.LogWarning("Concurrency conflict while saving card {CardId}", cardId);
                throw new ConflictException($"Card with id '{cardId}' was modified by another request. Reload and try again.");
            }
        }

        private async Task<(string Last4, string Hash, string Encrypted)> GenerateUniqueCardNumberAsync(CancellationToken cancellationToken)
        {
            for (var attempt = 1; attempt <= MaxCardNumberAttempts; attempt++)
            {
                var generated = _cardEncryptionService.GenerateCardNumber();

                var exists = await _cardRepository
                    .GetByExpression(asNoTracking: true, expression: c => c.CardNumberHash == generated.Hash)
                    .AnyAsync(cancellationToken);

                if (!exists)
                    return generated;

                _logger.LogWarning("Generated card number collided with an existing card (attempt {Attempt})", attempt);
            }

            throw new InvalidOperationException("Could not generate a unique card number.");
        }

        private static NotFoundException CardNotFound(int id) =>
            new($"Card with id '{id}' was not found.");

        private static NotFoundException AccountNotFound(int id) =>
            new($"Account with id '{id}' was not found.");
    }
}