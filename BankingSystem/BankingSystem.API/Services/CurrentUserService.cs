using BankingSystem.Business.Exceptions;
using BankingSystem.Business.Interfaces;
using System.Security.Claims;

namespace BankingSystem.API.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public string UserId =>
            User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedException("User is not authenticated.");

        public bool IsAdmin => User?.IsInRole("Admin") ?? false;
    }
}
