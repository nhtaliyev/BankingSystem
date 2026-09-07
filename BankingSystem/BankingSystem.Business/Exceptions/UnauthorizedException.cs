using Microsoft.AspNetCore.Http;

namespace BankingSystem.Business.Exceptions
{
    // the caller isn't authenticated at all (no/invalid/expired token)
    public class UnauthorizedException : AppException
    {
        public UnauthorizedException(string message) : base(message, StatusCodes.Status401Unauthorized) 
        {
        }

    }
}
