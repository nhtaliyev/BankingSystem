namespace BankingSystem.Business.Exceptions
{
    public class BadRequestException : Exception
    {
        public string PropName { get; set; }

        public BadRequestException()
        {
        }

        public BadRequestException(string? message) : base(message)
        {
        }

        public BadRequestException(string propname, string? message) : base(message)
        {
            PropName = propname;
        }
    }
}
