using Microsoft.AspNetCore.Http;

namespace BankingSystem.Business.Exceptions
{
    // the request conflicts with existing state, like a duplicate account number or a concurrency clash on update.
    public class ConflictException : AppException
    {
        public ConflictException(string message) : base(message, StatusCodes.Status409Conflict)
        {
        }
    }
}
