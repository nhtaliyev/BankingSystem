namespace BankingSystem.Business.ExternalServices.Interfaces
{
    public interface ICardEncryptionService
    {
        (string Last4, string Hash, string Encrypted) GenerateCardNumber();
        string ComputeHash(string cardNumber);
        bool VerifyCardNumber(string cardNumber, string storedHash);
        string Decrypt(string encrypted);
    }
}