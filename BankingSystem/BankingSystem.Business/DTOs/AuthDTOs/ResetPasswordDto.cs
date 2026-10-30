using FluentValidation;

namespace BankingSystem.Business.DTOs.AuthDTOs
{
    public record ResetPasswordDto(string Email, string Token, string NewPassword, string ConfirmNewPassword);

    public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
    {
        public ResetPasswordDtoValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.Token).NotEmpty();
            RuleFor(x => x.NewPassword).MeetsPasswordPolicy();
            RuleFor(x => x.ConfirmNewPassword)
                .Equal(x => x.NewPassword).WithMessage("New password and confirmation don't match");
        }
    }
}
