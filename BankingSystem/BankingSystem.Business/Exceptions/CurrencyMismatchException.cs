using Microsoft.AspNetCore.Http;

namespace BankingSystem.Business.Exceptions
{
    public class CurrencyMismatchException : AppException
    {
        public CurrencyMismatchException(string message) : base(message, StatusCodes.Status400BadRequest)
        {
        }
    }
}
