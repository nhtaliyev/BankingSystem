using FluentValidation;

namespace BankingSystem.Business.DTOs.AuthDTOs
{
    public static class PasswordRules
    {
        public static IRuleBuilderOptions<T, string> MeetsPasswordPolicy<T>(this IRuleBuilder<T, string> rule) =>
            rule
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(10).WithMessage("Password must be at least 10 characters")
                .MaximumLength(64).WithMessage("Password must be at most 64 characters")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"\d").WithMessage("Password must contain at least one digit")
                .Matches(@"[\W_]").WithMessage("Password must contain at least one special character");
    }
}
