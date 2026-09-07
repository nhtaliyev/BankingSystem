using BankingSystem.Business.DTOs.UserDTOs;
using BankingSystem.Business.PaginatedLists;

namespace BankingSystem.Business.Interfaces
{
    public interface IUserService
    {
        Task<PagedResult<UserGetDto>> GetAllUsersAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
        Task<UserGetDto> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task UpdateUserAsync(string id, UserEditDto dto, CancellationToken cancellationToken = default);
        Task DeactivateUserAsync(string id, CancellationToken cancellationToken = default);

        Task AssignRoleAsync(string userId, string role, CancellationToken cancellationToken = default);
        Task<ICollection<string>> GetUserRolesAsync(string userId, CancellationToken cancellationToken = default);
    }
}