namespace BankingSystem.Business.Exceptions
{
    public class NotFoundException : Exception
    {
        public string PropName { get; set; }

        public NotFoundException()
        {
        }

        public NotFoundException(string? message) : base(message)
        {
        }

        public NotFoundException(string propname, string? message) : base(message)
        {
            PropName = propname;
        }
    }
}
