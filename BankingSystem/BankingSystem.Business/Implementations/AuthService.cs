using BankingSystem.Business.DTOs.AuthDTOs;
using BankingSystem.Business.DTOs.TokenDtos;
using BankingSystem.Business.Exceptions;
using BankingSystem.Business.Interfaces;
using BankingSystem.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BankingSystem.Business.Implementations
{
    public class AuthService : IAuthService
    {
        private const string CustomerRole = "Customer";
        private const int AccessTokenLifetimeMinutes = 30;
        private const int RefreshTokenLifetimeDays = 7;
        private const int MinSecretKeyBytes = 32;

        // Same message for "unknown email" and "bad token" so the reset endpoint can't be used to find registered emails.
        private const string InvalidResetMessage = "Invalid or expired password reset request.";

        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ICurrentUserService _currentUser;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IConfiguration configuration,
            ICurrentUserService currentUser,
            TimeProvider timeProvider,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _currentUser = currentUser;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

        // ───────────────────────────── Register ─────────────────────────────

        public async Task RegisterAsync(UserRegisterDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.Birthday.Date >= UtcNow.Date)
                throw new BusinessValidationException("Invalid birth date");

            var usedEmail = await _userManager.FindByEmailAsync(dto.Email);
            if (usedEmail != null)
                throw new ConflictException("This email is already used");

            var phoneInUse = await _userManager.Users
                .AnyAsync(x => x.PhoneNumber == dto.PhoneNumber, cancellationToken);
            if (phoneInUse)
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
                _logger.LogWarning(
                    "Registration failed for {MaskedEmail}: {ErrorCodes}",
                    MaskEmail(dto.Email), ErrorCodes(result));

                throw new BusinessValidationException($"Registration failed: {Describe(result)}");
            }

            await AssignCustomerRoleOrRollbackAsync(appUser);

            _logger.LogInformation("User {UserId} registered", appUser.Id);
        }

        // ────────────────────────────── Login ──────────────────────────────

        public async Task<TokenResponseDto> LoginAsync(UserLoginDto dto, CancellationToken cancellationToken = default)
        {
            var appUser = await _userManager.FindByEmailAsync(dto.Email);
            if (appUser == null)
            {
                _userManager.PasswordHasher.HashPassword(new AppUser(), dto.Password);

                _logger.LogWarning("Failed login: unknown email {MaskedEmail}", MaskEmail(dto.Email));
                throw new UnauthorizedException("Invalid credentials");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(appUser, dto.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    _logger.LogWarning("Failed login: account of user {UserId} is locked out", appUser.Id);
                else if (result.IsNotAllowed)
                    _logger.LogWarning("Failed login: user {UserId} is not allowed to sign in (e.g. unconfirmed account)", appUser.Id);
                else
                    _logger.LogWarning("Failed login: wrong password for user {UserId}", appUser.Id);

                throw new UnauthorizedException("Invalid credentials");
            }

            var token = await IssueTokensAsync(appUser);

            _logger.LogInformation("User {UserId} logged in", appUser.Id);
            return token;
        }

        // ──────────────────────────── Refresh token ────────────────────────────

        public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenDto dto, CancellationToken cancellationToken = default)
        {
            var principal = GetPrincipalFromExpiredToken(dto.AccessToken);
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedException("Invalid token");

            var appUser = await _userManager.FindByIdAsync(userId);
            if (appUser == null || !IsRefreshTokenValid(appUser, dto.RefreshToken))
            {
                _logger.LogWarning("Rejected refresh token for user {UserId}: unknown, expired or already rotated", userId);
                throw new UnauthorizedException("Invalid or expired refresh token");
            }

            if (await _userManager.IsLockedOutAsync(appUser))
            {
                _logger.LogWarning("Refresh refused: account of user {UserId} is locked out", appUser.Id);
                throw new UnauthorizedException("Invalid or expired refresh token");
            }

            var token = await IssueTokensAsync(appUser);

            _logger.LogInformation("Tokens refreshed for user {UserId}", appUser.Id);
            return token;
        }

        // ───────────────────────────── Logout ─────────────────────────────

        public async Task LogoutAsync(string userId, CancellationToken cancellationToken = default)
        {
            EnsureSelf(userId);

            var appUser = await _userManager.FindByIdAsync(userId)
                ?? throw new NotFoundException("User not found");

            var result = await RevokeRefreshTokenAsync(appUser);
            if (!result.Succeeded)
                throw new InvalidOperationException("Logout could not be completed.");

            _logger.LogInformation("User {UserId} logged out", userId);
        }

        // ─────────────────────────── Password flows ───────────────────────────

        public async Task ChangePasswordAsync(string userId, ChangePasswordDto dto, CancellationToken cancellationToken = default)
        {
            EnsureSelf(userId);

            if (dto.NewPassword != dto.ConfirmNewPassword)
                throw new BusinessValidationException("New password and confirmation do not match");

            var appUser = await _userManager.FindByIdAsync(userId)
                ?? throw new NotFoundException("User not found");

            var check = await _signInManager.CheckPasswordSignInAsync(appUser, dto.CurrentPassword, lockoutOnFailure: true);
            if (!check.Succeeded)
            {
                _logger.LogWarning(
                    "Password change rejected for user {UserId}: current password check failed (locked out: {IsLockedOut})",
                    userId, check.IsLockedOut);

                throw new BusinessValidationException(check.IsLockedOut
                    ? "Too many failed attempts. Try again later."
                    : "Current password is incorrect.");
            }

            var result = await _userManager.ChangePasswordAsync(appUser, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Password change failed for user {UserId}: {ErrorCodes}", userId, ErrorCodes(result));
                throw new BusinessValidationException($"Failed to change password: {Describe(result)}");
            }

            // Sessions started with the old password must not survive the change.
            await RevokeAfterPasswordChangeAsync(appUser);

            _logger.LogInformation("Password changed for user {UserId}", userId);
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken cancellationToken = default)
        {
            var appUser = await _userManager.FindByEmailAsync(dto.Email);

            if (appUser == null)
            {
                _logger.LogInformation("Password reset requested for unknown email {MaskedEmail}", MaskEmail(dto.Email));
                return;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(appUser);

            // TODO: send `token` via email/notification service (IEmailService, etc.).
            //  - URL-encode it (WebEncoders.Base64UrlEncode) before putting it in a link.
            //  - NEVER log the token or the reset link.
            //  - Do not return the token directly to the caller.
            _ = token;

            _logger.LogInformation("Password reset requested for user {UserId}", appUser.Id);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.NewPassword != dto.ConfirmNewPassword)
                throw new BusinessValidationException("New password and confirmation do not match");

            var appUser = await _userManager.FindByEmailAsync(dto.Email);
            if (appUser == null)
            {
                _logger.LogWarning("Password reset attempted for unknown email {MaskedEmail}", MaskEmail(dto.Email));
                throw new BadRequestException(InvalidResetMessage);
            }

            var result = await _userManager.ResetPasswordAsync(appUser, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code == "InvalidToken"))
                {
                    _logger.LogWarning("Password reset rejected for user {UserId}: invalid or expired token", appUser.Id);
                    throw new BadRequestException(InvalidResetMessage);
                }

                // Only reachable with a valid token, so describing password-policy errors leaks nothing.
                throw new BusinessValidationException($"Failed to reset password: {Describe(result)}");
            }

            await RevokeAfterPasswordChangeAsync(appUser);
            await _userManager.ResetAccessFailedCountAsync(appUser);

            _logger.LogInformation("Password reset completed for user {UserId}", appUser.Id);
        }

        public async Task ConfirmEmailAsync(ConfirmEmailDto dto, CancellationToken cancellationToken = default)
        {
            // Unknown user and bad token give the same answer, so user ids can't be probed.
            var appUser = await _userManager.FindByIdAsync(dto.UserId);
            if (appUser == null)
                throw new BusinessValidationException("Failed to confirm email");

            var result = await _userManager.ConfirmEmailAsync(appUser, dto.Token);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Email confirmation failed for user {UserId}: {ErrorCodes}", appUser.Id, ErrorCodes(result));
                throw new BusinessValidationException("Failed to confirm email");
            }

            _logger.LogInformation("Email confirmed for user {UserId}", appUser.Id);
        }

        // ───────────────────────────── Helpers ─────────────────────────────

        private async Task AssignCustomerRoleOrRollbackAsync(AppUser appUser)
        {
            IdentityResult roleResult;
            try
            {
                roleResult = await _userManager.AddToRoleAsync(appUser, CustomerRole);
            }
            catch (InvalidOperationException ex)
            {
                // Identity throws (instead of returning Failed) when the role doesn't exist.
                roleResult = IdentityResult.Failed(new IdentityError { Code = "RoleMissing", Description = ex.Message });
            }

            if (roleResult.Succeeded)
                return;

            _logger.LogError(
                "Could not assign role {Role} to new user {UserId}: {ErrorCodes}. Rolling the registration back",
                CustomerRole, appUser.Id, ErrorCodes(roleResult));

            // Without this the user would exist with no role at all.
            var deleteResult = await _userManager.DeleteAsync(appUser);
            if (!deleteResult.Succeeded)
                _logger.LogCritical("Rollback failed: user {UserId} exists without a role", appUser.Id);

            throw new InvalidOperationException("Registration could not be completed.");
        }

        private async Task<TokenResponseDto> IssueTokensAsync(AppUser appUser)
        {
            var roles = await _userManager.GetRolesAsync(appUser);
            var token = GenerateToken(appUser, roles);

            // Only a hash of the refresh token is stored: a leaked database must not leak usable sessions.
            appUser.RefreshToken = HashToken(token.RefreshToken);
            appUser.RefreshTokenExpiryTime = UtcNow.AddDays(RefreshTokenLifetimeDays);

            var result = await _userManager.UpdateAsync(appUser);
            if (!result.Succeeded)
            {
                // Two parallel refreshes with the same token: the loser lands here.
                if (result.Errors.Any(e => e.Code == "ConcurrencyFailure"))
                {
                    _logger.LogWarning("Concurrent token rotation detected for user {UserId}", appUser.Id);
                    throw new UnauthorizedException("Invalid or expired refresh token");
                }

                _logger.LogError("Failed to save refresh token for user {UserId}: {ErrorCodes}", appUser.Id, ErrorCodes(result));
                throw new InvalidOperationException("The session could not be created.");
            }

            return token;
        }

        private TokenResponseDto GenerateToken(AppUser appUser, IList<string> roles)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, appUser.Id),
                new(ClaimTypes.Name, appUser.FullName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var signingCredentials = new SigningCredentials(GetSigningKey(), SecurityAlgorithms.HmacSha256);

            var now = UtcNow;
            var expiresAt = now.AddMinutes(AccessTokenLifetimeMinutes);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _configuration["JWT:issuer"],
                audience: _configuration["JWT:audience"],
                claims: claims,
                notBefore: now,
                expires: expiresAt,
                signingCredentials: signingCredentials);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            var refreshToken = GenerateRefreshToken();

            return new TokenResponseDto(accessToken, expiresAt, refreshToken);
        }

        private bool IsRefreshTokenValid(AppUser appUser, string providedRefreshToken)
        {
            if (string.IsNullOrEmpty(appUser.RefreshToken) || string.IsNullOrEmpty(providedRefreshToken))
                return false;

            // Explicit null check: "null <= now" is false in C#, which would let a token without an expiry through.
            if (appUser.RefreshTokenExpiryTime is null || appUser.RefreshTokenExpiryTime <= UtcNow)
                return false;

            var stored = Encoding.UTF8.GetBytes(appUser.RefreshToken);
            var provided = Encoding.UTF8.GetBytes(HashToken(providedRefreshToken));

            return CryptographicOperations.FixedTimeEquals(stored, provided);
        }

        private async Task<IdentityResult> RevokeRefreshTokenAsync(AppUser appUser)
        {
            appUser.RefreshToken = null;
            appUser.RefreshTokenExpiryTime = null;
            return await _userManager.UpdateAsync(appUser);
        }

        private async Task RevokeAfterPasswordChangeAsync(AppUser appUser)
        {
            var result = await RevokeRefreshTokenAsync(appUser);
            if (!result.Succeeded)
                _logger.LogError("Could not revoke refresh token of user {UserId} after a password change: {ErrorCodes}",
                    appUser.Id, ErrorCodes(result));
        }

        private void EnsureSelf(string userId)
        {
            if (userId == _currentUser.UserId)
                return;

            _logger.LogWarning(
                "Access denied: user {UserId} attempted an account action on behalf of user {TargetUserId}",
                _currentUser.UserId, userId);

            throw new ForbiddenException("You can only perform this action for your own account.");
        }

        private SymmetricSecurityKey GetSigningKey()
        {
            var secretKey = _configuration["JWT:secretKey"]
                ?? throw new InvalidOperationException("JWT:secretKey is not configured");

            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            if (keyBytes.Length < MinSecretKeyBytes)
                throw new InvalidOperationException($"JWT:secretKey must be at least {MinSecretKeyBytes} bytes long.");

            return new SymmetricSecurityKey(keyBytes);
        }

        private static string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            RandomNumberGenerator.Fill(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private static string HashToken(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _configuration["JWT:issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JWT:audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = GetSigningKey(),
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }, // blocks algorithm-confusion attacks
                ValidateLifetime = false // expired tokens are expected here
            };

            try
            {
                return new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
            }
            catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
            {
                // Garbage or tampered tokens throw here; without this they would surface as a 500.
                _logger.LogWarning("Rejected an invalid access token during refresh ({Reason})", ex.GetType().Name);
                throw new UnauthorizedException("Invalid token");
            }
        }

        private static string Describe(IdentityResult result) =>
            string.Join("; ", result.Errors.Select(e => e.Description));

        // Codes ("DuplicateEmail", "PasswordTooShort", ...) are safe to log; descriptions can echo user input.
        private static string ErrorCodes(IdentityResult result) =>
            string.Join(", ", result.Errors.Select(e => e.Code));

        // "nihat@example.com" -> "n***@example.com": enough to correlate failed logins, without storing full emails in logs.
        private static string MaskEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return "(empty)";

            var at = email.IndexOf('@');
            return at <= 0 ? "***" : $"{email[0]}***{email[at..]}";
        }
    }
}