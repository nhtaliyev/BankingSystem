using FluentValidation;

namespace BankingSystem.Business.DTOs.AuthDTOs
{
    public record ConfirmEmailDto(string UserId, string Token);

    public class ConfirmEmailDtoValidator : AbstractValidator<ConfirmEmailDto>
    {
        public ConfirmEmailDtoValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Token).NotEmpty();
        }
    }

}
