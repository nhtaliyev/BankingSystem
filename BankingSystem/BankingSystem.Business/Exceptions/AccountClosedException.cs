using Microsoft.AspNetCore.Http;

namespace BankingSystem.Business.Exceptions
{
    public class AccountClosedException : AppException
    {
        public AccountClosedException(string message) : base(message, StatusCodes.Status422UnprocessableEntity)
        {
        }
    }
}
