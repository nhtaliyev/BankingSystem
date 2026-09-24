using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace BankingSystem.Business.DTOs.CardDTOs
{
    public record CardEditDto(decimal DailyLimit, byte[] RowVersion);

    public class CardEditDtoValidator : AbstractValidator<CardEditDto>
    {
        public CardEditDtoValidator()
        {
            RuleFor(x => x.DailyLimit).GreaterThan(0).WithMessage("Daily Limit must be greater than 0.");

            RuleFor(x => x.RowVersion).NotNull().NotEmpty();
        }
    }
}
