using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankingSystem.Business.DTOs.UserDTOs
{
    public record UserRegisterDto(string FullName, string Email, string Password, string ConfirmPassword,
                    string PhoneNumber, DateTime Birthday);

    public class UserRegisterDtoValidator : AbstractValidator<UserRegisterDto>
    {
        public UserRegisterDtoValidator()
        {
            RuleFor(x => x.FullName).NotNull().NotEmpty().MaximumLength(60);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .MaximumLength(40).WithMessage("Password must be less than 40 characters")
                .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches(@"\d").WithMessage("Password must contain at least one digit")
                .Matches(@"[\W_]").WithMessage("Password must contain at least one special character");


            RuleFor(x => x.Email).NotNull().NotEmpty();

            RuleFor(x => x.Birthday).NotEmpty().WithMessage("birthday cant be empty");

            RuleFor(x => x).Custom((x, context) =>
            {
                if (x.Password != x.ConfirmPassword)
                {
                    context.AddFailure("ConfirmPassword", "password and confirm password dont match");
                }
            });
        }
    }
}
