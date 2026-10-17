using Microsoft.AspNetCore.Http;

namespace BankingSystem.Business.Exceptions
{
    public class ExternalServiceException : AppException
    {
        public ExternalServiceException(string message) : base(message, StatusCodes.Status502BadGateway)
        {
        }
    }
}
