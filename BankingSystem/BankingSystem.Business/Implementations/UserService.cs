using BankingSystem.Business.DTOs.UserDTOs;
using BankingSystem.Business.Exceptions;
using BankingSystem.Business.Interfaces;
using BankingSystem.Business.PaginatedLists;
using BankingSystem.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankingSystem.Business.Implementations
{
    public class UserService : IUserService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserService(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        private static UserGetDto MapToDto(AppUser user) => new(
            user.Id,
            user.FullName,
            user.Email!,
            user.PhoneNumber!,
            user.Birthday
        );

        public async Task<PagedResult<UserGetDto>> GetAllUsersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var query = _userManager.Users.AsNoTracking().OrderBy(u => u.FullName);

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var userDtos = users.Select(MapToDto).ToList(); 

            return new PagedResult<UserGetDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<UserGetDto> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                ?? throw new NotFoundException($"User with id '{id}' was not found.");

            return MapToDto(user);
        }

        public async Task UpdateUserAsync(string id, UserEditDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(id)
                ?? throw new NotFoundException($"User with id '{id}' was not found.");

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            user.Birthday = dto.Birthday;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new BadRequestException($"Failed to update user: {errors}");
            }
        }

        public async Task DeactivateUserAsync(string id, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(id)
                ?? throw new NotFoundException($"User with id '{id}' was not found.");

            user.LockoutEnabled = true;
            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new BadRequestException($"Failed to deactivate user: {errors}");
            }
        }

        public async Task AssignRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new NotFoundException($"User with id '{userId}' was not found.");

            if (!await _roleManager.RoleExistsAsync(role))
                throw new BadRequestException($"Role '{role}' does not exist.");

            if (await _userManager.IsInRoleAsync(user, role))
                throw new ConflictException($"User already has role '{role}'.");

            var result = await _userManager.AddToRoleAsync(user, role);

            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new BadRequestException($"Failed to assign role: {errors}");
            }
        }

        public async Task<ICollection<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new NotFoundException($"User with id '{userId}' was not found.");

            return await _userManager.GetRolesAsync(user);
        }
    }
}