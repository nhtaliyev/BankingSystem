using Microsoft.AspNetCore.Http;

namespace BankingSystem.Business.Exceptions
{
    public class AccountFrozenException : AppException
    {
        public AccountFrozenException(string message) : base(message, StatusCodes.Status422UnprocessableEntity)
        {
        }
    }
}
