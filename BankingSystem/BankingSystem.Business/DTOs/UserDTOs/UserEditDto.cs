using FluentValidation;

namespace BankingSystem.Business.DTOs.UserDTOs
{
    public record UserEditDto(string FullName, string Email, string PhoneNumber, DateTime Birthday);

    public class UserEditDtoValidator : AbstractValidator<UserEditDto>
    {
        public UserEditDtoValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[1-9]\d{7,14}$").WithMessage("Phone number must be in international format.")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));
            RuleFor(x => x.Birthday)
                .LessThan(DateTime.UtcNow.Date).WithMessage("Birthday must be in the past.")
                .Must(b => b <= DateTime.UtcNow.Date.AddYears(-18)).WithMessage("You must be at least 18.");
        }
    }
}
