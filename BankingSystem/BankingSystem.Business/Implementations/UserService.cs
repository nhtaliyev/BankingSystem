using AutoMapper;
using BankingSystem.Business.DTOs.UserDTOs;
using BankingSystem.Business.Exceptions;
using BankingSystem.Business.Interfaces;
using BankingSystem.Business.PaginatedLists;
using BankingSystem.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankingSystem.Business.Implementations
{
    public class UserService : IUserService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ICurrentUserService _currentUser;
        private readonly TimeProvider _timeProvider;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ICurrentUserService currentUser,
            TimeProvider timeProvider,
            IMapper mapper,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _currentUser = currentUser;
            _timeProvider = timeProvider;
            _mapper = mapper;
            _logger = logger;
        }

        private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

        // ────────────────────────────── Read ──────────────────────────────

        public async Task<PagedResult<UserGetDto>> GetAllUsersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            EnsureAdmin("list users", target: null);

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _userManager.Users
                .AsNoTracking()
                .OrderBy(u => u.FullName)
                .ThenBy(u => u.Id);

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<UserGetDto>
            {
                Items = _mapper.Map<List<UserGetDto>>(users),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<UserGetDto> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            EnsureSelfOrAdmin(id);

            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                ?? throw UserNotFound(id);

            return _mapper.Map<UserGetDto>(user);
        }

        public async Task<ICollection<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
        {
            EnsureSelfOrAdmin(userId);

            var user = await _userManager.FindByIdAsync(userId)
                ?? throw UserNotFound(userId);

            return await _userManager.GetRolesAsync(user);
        }

        // ───────────────────────────── Update ─────────────────────────────

        public async Task UpdateUserAsync(string id, UserEditDto dto, CancellationToken cancellationToken = default)
        {
            EnsureSelfOrAdmin(id);

            if (dto.Birthday.Date >= UtcNow.Date)
                throw new BusinessValidationException("Invalid birth date");

            var user = await _userManager.FindByIdAsync(id)
                ?? throw UserNotFound(id);

            var phoneChanged = !string.Equals(user.PhoneNumber, dto.PhoneNumber, StringComparison.Ordinal);
            if (phoneChanged)
            {
                var phoneInUse = await _userManager.Users
                    .AnyAsync(u => u.Id != id && u.PhoneNumber == dto.PhoneNumber, cancellationToken);

                if (phoneInUse)
                    throw new ConflictException("This phone number is already used");
            }

            user.FullName = dto.FullName;
            user.Birthday = DateTime.SpecifyKind(dto.Birthday.Date, DateTimeKind.Utc);

            var result = phoneChanged
                ? await _userManager.SetPhoneNumberAsync(user, dto.PhoneNumber)
                : await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                throw ToException(result, "Failed to update user");

            _logger.LogInformation(
                "Profile of user {TargetUserId} updated by user {ActorUserId} (phone changed: {PhoneChanged})",
                id, _currentUser.UserId, phoneChanged);
        }

        // ───────────────────────── Admin operations ─────────────────────────

        public async Task DeactivateUserAsync(string id, CancellationToken cancellationToken = default)
        {
            EnsureAdmin("deactivate", id);

            if (id == _currentUser.UserId)
                throw new BusinessValidationException("You cannot deactivate your own account.");

            var user = await _userManager.FindByIdAsync(id)
                ?? throw UserNotFound(id);

            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw ToException(result, "Failed to deactivate user");

            var stampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
                _logger.LogError("Could not update the security stamp of deactivated user {TargetUserId}", id);

            _logger.LogWarning("User {TargetUserId} deactivated by admin {ActorUserId}", id, _currentUser.UserId);
        }

        public async Task AssignRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
        {
            EnsureAdmin("assign a role to", userId);

            var user = await _userManager.FindByIdAsync(userId)
                ?? throw UserNotFound(userId);

            if (!await _roleManager.RoleExistsAsync(role))
                throw new BadRequestException($"Role '{role}' does not exist.");

            if (await _userManager.IsInRoleAsync(user, role))
                throw new ConflictException($"User already has role '{role}'.");

            var result = await _userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded)
                throw ToException(result, "Failed to assign role");

            _logger.LogWarning(
                "Role {Role} assigned to user {TargetUserId} by admin {ActorUserId}",
                role, userId, _currentUser.UserId);
        }

        // ───────────────────────────── Helpers ─────────────────────────────

        private void EnsureAdmin(string action, string? target)
        {
            if (_currentUser.IsAdmin)
                return;

            _logger.LogWarning(
                "Access denied: user {UserId} tried to {Action} (target: {TargetUserId}) without the Admin role",
                _currentUser.UserId, action, target);

            throw new ForbiddenException("This action requires the Admin role.");
        }

        private void EnsureSelfOrAdmin(string targetUserId)
        {
            if (_currentUser.IsAdmin || targetUserId == _currentUser.UserId)
                return;

            _logger.LogWarning(
                "Access denied: user {UserId} tried to access user {TargetUserId}",
                _currentUser.UserId, targetUserId);

            throw UserNotFound(targetUserId);
        }

        private static Exception ToException(IdentityResult result, string message)
        {
            if (result.Errors.Any(e => e.Code == "ConcurrencyFailure"))
                return new ConflictException("The user was modified by another request. Reload and try again.");

            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return new BadRequestException($"{message}: {errors}");
        }

        private static NotFoundException UserNotFound(string id) =>
            new($"User with id '{id}' was not found.");
    }
}