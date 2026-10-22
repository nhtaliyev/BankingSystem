using BankingSystem.Business.DTOs.UserDTOs;
using BankingSystem.Business.Interfaces;
using BankingSystem.Business.PaginatedLists;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BankingSystem.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUser;

        public UsersController(IUserService userService, ICurrentUserService currentUser)
        {
            _userService = userService;
            _currentUser = currentUser;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PagedResult<UserGetDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResult<UserGetDto>>> GetAll(
            int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            return Ok(await _userService.GetAllUsersAsync(page, pageSize, cancellationToken));
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(UserGetDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<UserGetDto>> GetMe(CancellationToken cancellationToken)
        {
            return Ok(await _userService.GetByIdAsync(_currentUser.UserId, cancellationToken));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserGetDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserGetDto>> GetById(string id, CancellationToken cancellationToken)
        {
            return Ok(await _userService.GetByIdAsync(id, cancellationToken));
        }

        [HttpGet("{id}/roles")]
        [ProducesResponseType(typeof(ICollection<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ICollection<string>>> GetRoles(string id, CancellationToken cancellationToken)
        {
            return Ok(await _userService.GetUserRolesAsync(id, cancellationToken));
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Update(string id, UserEditDto dto, CancellationToken cancellationToken)
        {
            await _userService.UpdateUserAsync(id, dto, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deactivate(string id, CancellationToken cancellationToken)
        {
            await _userService.DeactivateUserAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPost("{id}/roles/{role}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AssignRole(string id, string role, CancellationToken cancellationToken)
        {
            await _userService.AssignRoleAsync(id, role, cancellationToken);
            return NoContent();
        }
    }
}