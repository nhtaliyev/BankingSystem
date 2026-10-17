namespace BankingSystem.Business.Interfaces
{
    public interface ICurrentUserService
    {
        string UserId { get; }

        bool IsAdmin { get; }
    }
}