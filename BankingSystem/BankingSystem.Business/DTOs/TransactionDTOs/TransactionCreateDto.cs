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
        Currency? Currency,
        //TransactionType Type,
        string? Description
    );

    public class TransactionCreateDtoValidator : AbstractValidator<TransactionCreateDto>
    {
        public TransactionCreateDtoValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than 0.")
                .LessThanOrEqualTo(1_000_000m).WithMessage("Amount exceeds the maximum allowed.")
                .PrecisionScale(18, 2, true).WithMessage("Amount can have at most 2 decimal places.");

            RuleFor(x => x.Currency).NotNull().IsInEnum();
            RuleFor(x => x.Description).MaximumLength(250);

            RuleFor(x => x)
                .Must(x => x.FromAccountId.HasValue ^ x.FromCardId.HasValue)
                .WithMessage("Specify exactly one source: an account or a card.")
                .Must(x => x.ToAccountId.HasValue ^ x.ToCardId.HasValue)
                .WithMessage("Specify exactly one destination: an account or a card.")
                .Must(x => !(x.FromAccountId.HasValue && x.FromAccountId == x.ToAccountId)
                        && !(x.FromCardId.HasValue && x.FromCardId == x.ToCardId))
                .WithMessage("Source and destination cannot be the same.");
        }
    }
}
