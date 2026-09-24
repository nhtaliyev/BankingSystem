using BankingSystem.Business.DTOs.CardDTOs;
using BankingSystem.Core.Enums;
using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace BankingSystem.Business.DTOs.TransactionDTOs
{
    public record TransactionCreateDto(
        int? FromAccountId,
        int? ToAccountId,
        int? FromCardId,
        int? ToCardId,
        decimal Amount,
        Currency Currency,
        TransactionType Type,
        string? Description
    );

    public class TransactionCreateDtoValidator : AbstractValidator<TransactionCreateDto>
    {
        public TransactionCreateDtoValidator()
        {
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than 0.");
        }
    }
}
