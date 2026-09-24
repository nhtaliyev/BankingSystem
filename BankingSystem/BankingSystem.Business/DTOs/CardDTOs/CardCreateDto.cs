using FluentValidation;
using System.ComponentModel.DataAnnotations;

namespace BankingSystem.Business.DTOs.CardDTOs
{
    public record CardCreateDto(int AccountId, decimal DailyLimit);

    public class CardCreateDtoValidator : AbstractValidator<CardCreateDto>
    {
        public CardCreateDtoValidator()
        {
            RuleFor(x => x.DailyLimit).GreaterThan(0).WithMessage("Daily Limit must be greater than 0.");
        }
    }
}
