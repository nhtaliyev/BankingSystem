using Microsoft.AspNetCore.Http;

namespace BankingSystem.Business.Exceptions
{
    public class BadRequestException : AppException
    {
        public BadRequestException(string message) : base(message, StatusCodes.Status400BadRequest) 
        {
        }

    }
}
