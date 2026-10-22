using BankingSystem.Business.DTOs.AuthDTOs;
using BankingSystem.Business.DTOs.TokenDtos;
using BankingSystem.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BankingSystem.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Authorize]
    [EnableRateLimiting("auth")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ICurrentUserService _currentUser;

        public AuthController(IAuthService authService, ICurrentUserService currentUser)
        {
            _authService = authService;
            _currentUser = currentUser;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Register(UserRegisterDto dto, CancellationToken cancellationToken)
        {
            await _authService.RegisterAsync(dto, cancellationToken);
            return NoContent();
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TokenResponseDto>> Login(UserLoginDto dto, CancellationToken cancellationToken)
        {
            return Ok(await _authService.LoginAsync(dto, cancellationToken));
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TokenResponseDto>> Refresh(RefreshTokenDto dto, CancellationToken cancellationToken)
        {
            return Ok(await _authService.RefreshTokenAsync(dto, cancellationToken));
        }

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            await _authService.LogoutAsync(_currentUser.UserId, cancellationToken);
            return NoContent();
        }

        [HttpPost("change-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto, CancellationToken cancellationToken)
        {
            await _authService.ChangePasswordAsync(_currentUser.UserId, dto, cancellationToken);
            return NoContent();
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto, CancellationToken cancellationToken)
        {
            await _authService.ForgotPasswordAsync(dto, cancellationToken);
            return NoContent();
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto, CancellationToken cancellationToken)
        {
            await _authService.ResetPasswordAsync(dto, cancellationToken);
            return NoContent();
        }

        // POST, not GET: confirming changes state, and GET requests get prefetched by mail scanners and browsers.
        // The email link opens a frontend page, and that page POSTs userId + token here.
        [HttpPost("confirm-email")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailDto dto, CancellationToken cancellationToken)
        {
            await _authService.ConfirmEmailAsync(dto, cancellationToken);
            return NoContent();
        }
    }
}