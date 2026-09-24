using AutoMapper;
using BankingSystem.Business.DTOs.CardDTOs;
using BankingSystem.Business.Exceptions;
using BankingSystem.Business.ExternalServices.Interfaces;
using BankingSystem.Business.Interfaces;
using BankingSystem.Core.Enums;
using BankingSystem.Core.Models;
using BankingSystem.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Business.Implementations
{
    public class CardService : ICardService
    {
        private readonly ICardRepository _cardRepository;
        private readonly IGenericRepository<Account> _accountRepository;
        private readonly ICardEncryptionService _cardEncryptionService;
        private readonly IMapper _mapper;

        public CardService(
            ICardRepository cardRepository,
            IGenericRepository<Account> accountRepository,
            ICardEncryptionService cardEncryptionService,
            IMapper mapper)
        {
            _cardRepository = cardRepository;
            _accountRepository = accountRepository;
            _cardEncryptionService = cardEncryptionService;
            _mapper = mapper;
        }

        public async Task<CardGetDto> CreateCardAsync(CardCreateDto dto, CancellationToken cancellationToken = default)
        {
            var account = await _accountRepository.GetByIdAsync(dto.AccountId, cancellationToken)
                ?? throw new NotFoundException($"Account with id '{dto.AccountId}' was not found.");

            var card = _mapper.Map<Card>(dto);

            var (last4, hash, encrypted) = _cardEncryptionService.GenerateCardNumber();
            card.CardNumberLast4 = last4;
            card.CardNumberHash = hash;
            card.CardNumberEncrypted = encrypted;
            card.ExpiryDate = DateTime.UtcNow.AddYears(4);
            card.Status = CardStatus.Active;
            card.Balance = 0m;

            await _cardRepository.AddAsync(card, cancellationToken);
            await _cardRepository.CommitAsync(cancellationToken);

            return _mapper.Map<CardGetDto>(card);
        }

        public async Task<CardGetDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var card = await _cardRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Card with id '{id}' was not found.");

            return _mapper.Map<CardGetDto>(card);
        }

        public async Task<ICollection<CardGetDto>> GetByAccountIdAsync(int accountId, CancellationToken cancellationToken = default)
        {
            var cards = await _cardRepository
                .GetByExpression(asNoTracking: true, expression: c => c.AccountId == accountId)
                .ToListAsync(cancellationToken);

            return _mapper.Map<ICollection<CardGetDto>>(cards);
        }

        public async Task UpdateCardAsync(int id, CardEditDto dto, CancellationToken cancellationToken = default)
        {
            var card = await _cardRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Card with id '{id}' was not found.");

            EnsureNotClosed(card);

            _mapper.Map(dto, card);

            await _cardRepository.SetOriginalRowVersionAsync(card, dto.RowVersion, cancellationToken);

            try
            {
                await _cardRepository.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException($"Card with id '{id}' was modified by another request. Reload and try again.");
            }
        }

        public async Task FreezeCardAsync(int id, CancellationToken cancellationToken = default)
        {
            var card = await _cardRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Card with id '{id}' was not found.");

            EnsureNotClosed(card);

            if (card.Status == CardStatus.Blocked)
                throw new BusinessValidationException($"Card with id '{id}' is blocked and must be unblocked before it can be frozen.");

            if (card.Status == CardStatus.Expired)
                throw new BusinessValidationException($"Card with id '{id}' is expired and cannot be frozen.");

            if (card.Status == CardStatus.Frozen)
                throw new ConflictException($"Card with id '{id}' is already frozen.");

            card.Status = CardStatus.Frozen;
            await _cardRepository.CommitAsync(cancellationToken);
        }

        public async Task UnfreezeCardAsync(int id, CancellationToken cancellationToken = default)
        {
            var card = await _cardRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Card with id '{id}' was not found.");

            EnsureNotClosed(card);

            if (card.Status != CardStatus.Frozen)
                throw new BusinessValidationException($"Card with id '{id}' is not frozen (current status: {card.Status}).");

            card.Status = CardStatus.Active;
            await _cardRepository.CommitAsync(cancellationToken);
        }

        public async Task BlockCardAsync(int id, CancellationToken cancellationToken = default)
        {
            var card = await _cardRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Card with id '{id}' was not found.");

            EnsureNotClosed(card);

            if (card.Status == CardStatus.Expired)
                throw new BusinessValidationException($"Card with id '{id}' is expired and cannot be blocked.");

            if (card.Status == CardStatus.Blocked)
                throw new ConflictException($"Card with id '{id}' is already blocked.");

            card.Status = CardStatus.Blocked;
            await _cardRepository.CommitAsync(cancellationToken);
        }

        public async Task UnblockCardAsync(int id, CancellationToken cancellationToken = default)
        {
            var card = await _cardRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Card with id '{id}' was not found.");

            EnsureNotClosed(card);

            if (card.Status != CardStatus.Blocked)
                throw new BusinessValidationException($"Card with id '{id}' is not blocked (current status: {card.Status}).");

            card.Status = CardStatus.Active;
            await _cardRepository.CommitAsync(cancellationToken);
        }

        public async Task CloseCardAsync(int id, CancellationToken cancellationToken = default)
        {
            var card = await _cardRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Card with id '{id}' was not found.");

            if (card.Status == CardStatus.Closed)
                throw new ConflictException($"Card with id '{id}' is already closed.");

            if (card.Balance != 0m)
                throw new BusinessValidationException("Card must have a zero balance before it can be closed.");

            card.Status = CardStatus.Closed;
            await _cardRepository.CommitAsync(cancellationToken);
        }

        public async Task<CardNumberRevealDto> RevealCardNumberAsync(int id, CancellationToken cancellationToken = default)
        {
            var card = await _cardRepository.GetByIdAsync(id, cancellationToken)
                ?? throw new NotFoundException($"Card with id '{id}' was not found.");

            var cardNumber = _cardEncryptionService.Decrypt(card.CardNumberEncrypted);
            return new CardNumberRevealDto(cardNumber);
        }

        private static void EnsureNotClosed(Card card)
        {
            if (card.Status == CardStatus.Closed)
                throw new BusinessValidationException($"Card with id '{card.Id}' is closed and cannot be modified.");
        }
    }
}