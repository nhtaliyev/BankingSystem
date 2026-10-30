using FluentValidation;

namespace BankingSystem.Business.DTOs.AuthDTOs
{
    public record UserRegisterDto(string FullName, string Email, string Password, string ConfirmPassword,
                    string PhoneNumber, DateTime Birthday);

    public class UserRegisterDtoValidator : AbstractValidator<UserRegisterDto>
    {
        public UserRegisterDtoValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);

            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);

            RuleFor(x => x.PhoneNumber)
                .NotEmpty()
                .Matches(@"^\+?[1-9]\d{7,14}$").WithMessage("Phone number must be in international format.");

            RuleFor(x => x.Password).MeetsPasswordPolicy();

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Password and confirm password don't match");

            RuleFor(x => x.Birthday)
                .NotEmpty().WithMessage("Birthday can't be empty")
                .Must(b => b.Date <= DateTime.UtcNow.Date.AddYears(-18)).WithMessage("You must be at least 18.");
        }
    }
}
