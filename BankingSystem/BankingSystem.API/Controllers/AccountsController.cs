using BankingSystem.Business.DTOs.AccountDTOs;
using BankingSystem.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystem.API.Controllers
{
    [ApiController]
    [Route("api/accounts")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ICurrentUserService _currentUser;

        public AccountsController(IAccountService accountService, ICurrentUserService currentUser)
        {
            _accountService = accountService;
            _currentUser = currentUser;
        }

        [HttpPost]
        [ProducesResponseType(typeof(AccountGetDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AccountGetDto>> Create(
            AccountCreateDto dto, CancellationToken cancellationToken)
        {
            var account = await _accountService.CreateAccountAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(AccountGetDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<AccountGetDto>> GetById(
            int id, CancellationToken cancellationToken)
        {
            return Ok(await _accountService.GetByIdAsync(id, cancellationToken));
        }

        [HttpGet("my")]
        [ProducesResponseType(typeof(ICollection<AccountGetDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ICollection<AccountGetDto>>> GetMine(
            CancellationToken cancellationToken)
        {
            return Ok(await _accountService.GetByUserIdAsync(_currentUser.UserId, cancellationToken));
        }

        [HttpGet("user/{appUserId}")]
        [ProducesResponseType(typeof(ICollection<AccountGetDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ICollection<AccountGetDto>>> GetByUserId(
            string appUserId, CancellationToken cancellationToken)
        {
            return Ok(await _accountService.GetByUserIdAsync(appUserId, cancellationToken));
        }

        [HttpPost("{id:int}/freeze")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Freeze(int id, CancellationToken cancellationToken)
        {
            await _accountService.FreezeAccountAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/unfreeze")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Unfreeze(int id, CancellationToken cancellationToken)
        {
            await _accountService.UnfreezeAccountAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/block")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Block(int id, CancellationToken cancellationToken)
        {
            await _accountService.BlockAccountAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/unblock")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Unblock(int id, CancellationToken cancellationToken)
        {
            await _accountService.UnblockAccountAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id:int}/close")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Close(int id, CancellationToken cancellationToken)
        {
            await _accountService.CloseAccountAsync(id, cancellationToken);
            return NoContent();
        }
    }
}