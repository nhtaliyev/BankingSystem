using BankingSystem.Business.DTOs.AuthDTOs;
using BankingSystem.Business.DTOs.TokenDtos;

namespace BankingSystem.Business.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(UserRegisterDto dto, CancellationToken cancellationToken = default);
        Task<TokenResponseDto> LoginAsync(UserLoginDto dto, CancellationToken cancellationToken = default);
        Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default);
        Task LogoutAsync(string userId, CancellationToken cancellationToken = default);


        Task ChangePasswordAsync(string userId, ChangePasswordDto dto, CancellationToken cancellationToken = default);
        Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default);
        Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default);

        Task ConfirmEmailAsync(ConfirmEmailDto dto, CancellationToken cancellationToken = default);
    }
}