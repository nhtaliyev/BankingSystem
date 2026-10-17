using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace BankingSystem.Business.DTOs.CardDTOs
{
    public record CardEditDto(decimal DailyLimit, byte[] RowVersion);

    public class CardEditDtoValidator : AbstractValidator<CardEditDto>
    {
        public CardEditDtoValidator()
        {
            RuleFor(x => x.DailyLimit)
                .GreaterThan(0)
                .LessThanOrEqualTo(10_000m)
                .PrecisionScale(18, 2, true);
            RuleFor(x => x.RowVersion).NotEmpty();
        }
    }
}
