using Microsoft.AspNetCore.Http;

namespace BankingSystem.Business.Exceptions
{
    public class ValidationException : AppException
    {
        public ValidationException(string message) : base(message, StatusCodes.Status400BadRequest)
        {
        }
    }
}
