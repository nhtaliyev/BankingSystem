using FluentValidation;

namespace BankingSystem.Business.DTOs.AuthDTOs
{
    public record ChangePasswordDto(string CurrentPassword, string NewPassword, string ConfirmNewPassword);

    public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordDtoValidator()
        {
            RuleFor(x => x.CurrentPassword).NotEmpty();
            RuleFor(x => x.NewPassword).MeetsPasswordPolicy();
            RuleFor(x => x.ConfirmNewPassword)
                .Equal(x => x.NewPassword).WithMessage("New password and confirmation don't match");
        }
    }

}
