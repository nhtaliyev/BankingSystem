using Microsoft.AspNetCore.Http;

namespace BankingSystem.Business.Exceptions
{
    public class NotFoundException : AppException
    {
        public NotFoundException(string message) : base(message, StatusCodes.Status404NotFound) 
        {
        }

    }
}
