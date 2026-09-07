using Microsoft.AspNetCore.Http;

namespace BankingSystem.Business.Exceptions
{
    // the caller is authenticated but isn't allowed to do this specific action (e.g. accessing someone else's account)
    public class ForbiddenException : AppException
    {
        public ForbiddenException(string message) : base(message, StatusCodes.Status403Forbidden)
        {
        }
    }
}
