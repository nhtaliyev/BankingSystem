using Microsoft.AspNetCore.Http;

namespace BankingSystem.Business.Exceptions
{
    public class DailyLimitExceededException : AppException
    {
        public DailyLimitExceededException(string message) : base(message, StatusCodes.Status422UnprocessableEntity)
        {
        }
    }
}
