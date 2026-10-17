using Microsoft.AspNetCore.Http;

namespace BankingSystem.Business.Exceptions
{
    public class CardClosedException : AppException
    {
        public CardClosedException(string message) : base(message, StatusCodes.Status400BadRequest)
        {
        }
    }
}
