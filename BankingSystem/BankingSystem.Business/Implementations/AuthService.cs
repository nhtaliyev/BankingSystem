using BankingSystem.Business.DTOs.AuthDTOs;
using BankingSystem.Business.DTOs.TokenDtos;
using BankingSystem.Business.Exceptions;
using BankingSystem.Business.Interfaces;
using BankingSystem.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BankingSystem.Business.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        public async Task RegisterAsync(UserRegisterDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.Birthday >= DateTime.Now)
                throw new BusinessValidationException("Invalid birth date");

            var usedEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (usedEmail != null)
                throw new ConflictException("This email is already used");

            var usedPhone = await _userManager.Users
                .FirstOrDefaultAsync(x => x.PhoneNumber == dto.PhoneNumber, cancellationToken);
            if (usedPhone != null)
                throw new ConflictException("This phone number is already used");

            var appUser = new AppUser
            {
                Email = dto.Email,
                Birthday = dto.Birthday,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                UserName = dto.Email,
            };

            var result = await _userManager.CreateAsync(appUser, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new BusinessValidationException($"Registration failed: {errors}");
            }

            await _userManager.AddToRoleAsync(appUser, "Customer");
        }

        public async Task<TokenResponseDto> LoginAsync(UserLoginDto dto, CancellationToken cancellationToken = default)
        {
            var appUser = await _userManager.FindByEmailAsync(dto.Email);
            if (appUser == null)
                throw new UnauthorizedException("Invalid credentials");

            var result = await _signInManager.CheckPasswordSignInAsync(appUser, dto.Password, dto.RememberMe);
            if (!result.Succeeded)
                throw new UnauthorizedException("Invalid credentials");

            var roles = await _userManager.GetRolesAsync(appUser);

            var token = await GenerateTokenAsync(appUser, roles);

            appUser.RefreshToken = token.RefreshToken;
            appUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(appUser);

            return token;
        }

        public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default)
        {
            var principal = GetPrincipalFromExpiredToken(dto.AccessToken);
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            var appUser = await _userManager.FindByIdAsync(userId);
            if (appUser == null
                || appUser.RefreshToken != dto.RefreshToken
                || appUser.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new UnauthorizedException("Invalid or expired refresh token");
            }

            var roles = await _userManager.GetRolesAsync(appUser);
            var token = await GenerateTokenAsync(appUser, roles);

            appUser.RefreshToken = token.RefreshToken;
            appUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(appUser);

            return token;
        }

        public async Task LogoutAsync(string userId, CancellationToken cancellationToken = default)
        {
            var appUser = await _userManager.FindByIdAsync(userId);
            if (appUser == null)
                throw new NotFoundException("User not found");

            appUser.RefreshToken = null;
            appUser.RefreshTokenExpiryTime = null;
            await _userManager.UpdateAsync(appUser);
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.NewPassword != dto.ConfirmNewPassword)
                throw new BusinessValidationException("New password and confirmation do not match");

            var appUser = await _userManager.FindByIdAsync(userId);
            if (appUser == null)
                throw new NotFoundException("User not found");

            var result = await _userManager.ChangePasswordAsync(appUser, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new BusinessValidationException($"Failed to change password: {errors}");
            }
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default)
        {
            var appUser = await _userManager.FindByEmailAsync(dto.Email);

            // Do not throw NotFoundException here - it lets attackers enumerate
            // which emails are registered. Silently no-op if the user doesn't exist.
            if (appUser == null)
                return;

            var token = await _userManager.GeneratePasswordResetTokenAsync(appUser);

            // TODO: send `token` via email/notification service (IEmailService, etc.)
            // Do not return the token directly to the caller.
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.NewPassword != dto.ConfirmNewPassword)
                throw new BusinessValidationException("New password and confirmation do not match");

            var appUser = await _userManager.FindByEmailAsync(dto.Email);
            if (appUser == null)
                throw new UnauthorizedException("Invalid reset request");

            var result = await _userManager.ResetPasswordAsync(appUser, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new BusinessValidationException($"Failed to reset password: {errors}");
            }
        }

        public async Task ConfirmEmailAsync(ConfirmEmailDto dto, CancellationToken cancellationToken = default)
        {
            var appUser = await _userManager.FindByIdAsync(dto.UserId);
            if (appUser == null)
                throw new NotFoundException("User not found");

            var result = await _userManager.ConfirmEmailAsync(appUser, dto.Token);
            if (!result.Succeeded)
                throw new BusinessValidationException("Failed to confirm email");
        }

        private async Task<TokenResponseDto> GenerateTokenAsync(AppUser appUser, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, appUser.Id),
                new Claim(ClaimTypes.Name, appUser.FullName),
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var secretKey = _configuration["JWT:secretKey"]
                ?? throw new InvalidOperationException("JWT:secretKey is not configured");

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var expiredAt = DateTime.UtcNow.AddMinutes(30); // short-lived access token
            var jwtSecurityToken = new JwtSecurityToken(
                signingCredentials: signingCredentials,
                claims: claims,
                audience: _configuration["JWT:audience"],
                issuer: _configuration["JWT:issuer"],
                expires: expiredAt,
                notBefore: DateTime.UtcNow
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            var refreshToken = GenerateRefreshToken();

            return new TokenResponseDto(accessToken, expiredAt, refreshToken);
        }

        private static string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            RandomNumberGenerator.Fill(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var secretKey = _configuration["JWT:secretKey"]
                ?? throw new InvalidOperationException("JWT:secretKey is not configured");

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _configuration["JWT:issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JWT:audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateLifetime = false // expired tokens are expected here
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new UnauthorizedException("Invalid token");
            }

            return principal;
        }
    }
}