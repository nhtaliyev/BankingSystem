using Microsoft.AspNetCore.Http;

namespace BankingSystem.Business.Exceptions
{
    // the transaction itself is malformed or nonsensical (negative amount, same-account transfer, etc.), regardless of balance or account state.
    public class InvalidTransactionException : AppException
    {
        public InvalidTransactionException(string message) : base(message, StatusCodes.Status400BadRequest)
        {
        }
    }
}
