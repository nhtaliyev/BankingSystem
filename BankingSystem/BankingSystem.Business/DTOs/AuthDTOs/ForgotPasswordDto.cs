using FluentValidation;

namespace BankingSystem.Business.DTOs.AuthDTOs
{
    public record ForgotPasswordDto(string Email);

    public class ForgotPasswordDtoValidator : AbstractValidator<ForgotPasswordDto>
    {
        public ForgotPasswordDtoValidator() =>
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }

}
