using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankingSystem.Business.Exceptions
{
    public class ExternalServiceException : AppException
    {
        public ExternalServiceException(string message) : base(message, StatusCodes.Status502BadGateway)
        {
        }
    }
}
