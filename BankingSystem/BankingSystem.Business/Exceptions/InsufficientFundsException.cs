using Microsoft.AspNetCore.Http;

namespace BankingSystem.Business.Exceptions
{
    public class InsufficientFundsException : AppException
    {
        public InsufficientFundsException(string message) : base(message, StatusCodes.Status422UnprocessableEntity)
        {
        }
    }
}
