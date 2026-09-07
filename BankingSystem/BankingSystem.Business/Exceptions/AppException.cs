using System;
using System.Collections.Generic;
using System.Text;

namespace BankingSystem.Business.Exceptions
{
    public abstract class AppException : Exception
    {
        public int StatusCode { get; }

        protected AppException(string message, int statusCode) : base(message)
        {
            StatusCode = statusCode;
        }

        protected AppException(string message, int statusCode, Exception innerException) : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}
