using BankingSystem.Core.Enums;
using FluentValidation;

namespace BankingSystem.Business.DTOs.AccountDTOs
{
    //public record AccountCreateDto(string AppUserId, Currency Currency);
    public record AccountCreateDto(Currency Currency);

    public class AccountCreateDtoValidator : AbstractValidator<AccountCreateDto>
    {
        public AccountCreateDtoValidator() => RuleFor(x => x.Currency).IsInEnum();
    }
}
