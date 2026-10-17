using Microsoft.AspNetCore.Http;

namespace BankingSystem.Business.Exceptions
{
    public class BusinessValidationException : AppException
    {
        public BusinessValidationException(string message) : base(message, StatusCodes.Status400BadRequest)
        {
        }
    }
}
