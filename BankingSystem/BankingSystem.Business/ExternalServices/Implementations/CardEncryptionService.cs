using BankingSystem.Business.ExternalServices.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace BankingSystem.Business.ExternalServices.Implementations
{
    public class CardEncryptionOptions
    {
        public string HashKey { get; set; } = null!;
        public string EncryptionKey { get; set; } = null!;
    }

    public class CardEncryptionService : ICardEncryptionService
    {
        private readonly byte[] _hashKey;
        private readonly byte[] _encryptionKey;

        public CardEncryptionService(IOptions<CardEncryptionOptions> options)
        {
            _hashKey = Convert.FromBase64String(options.Value.HashKey);
            _encryptionKey = Convert.FromBase64String(options.Value.EncryptionKey);

            if (_encryptionKey.Length != 32)
                throw new InvalidOperationException("Card encryption key must be 32 bytes (AES-256).");
        }

        public (string Last4, string Hash, string Encrypted) GenerateCardNumber()
        {
            var cardNumber = GenerateLuhnValidNumber();
            var last4 = cardNumber[^4..];
            var hash = ComputeHash(cardNumber);
            var encrypted = Encrypt(cardNumber);

            return (last4, hash, encrypted);
        }

        public string ComputeHash(string cardNumber)
        {
            using var hmac = new HMACSHA256(_hashKey);
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(cardNumber));
            return Convert.ToHexString(hashBytes);
        }

        public bool VerifyCardNumber(string cardNumber, string storedHash)
        {
            var computedHash = ComputeHash(cardNumber);

            var computedBytes = Convert.FromHexString(computedHash);
            var storedBytes = Convert.FromHexString(storedHash);

            return CryptographicOperations.FixedTimeEquals(computedBytes, storedBytes);
        }

        public string Decrypt(string encrypted)
        {
            var payload = Convert.FromBase64String(encrypted);

            var nonce = payload[..12];
            var tag = payload[12..28];
            var ciphertext = payload[28..];
            var plaintext = new byte[ciphertext.Length];

            using var aesGcm = new AesGcm(_encryptionKey, tagSizeInBytes: 16);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }

        private string Encrypt(string plainCardNumber)
        {
            var plaintext = Encoding.UTF8.GetBytes(plainCardNumber);
            var nonce = RandomNumberGenerator.GetBytes(12);
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[16];

            using var aesGcm = new AesGcm(_encryptionKey, tagSizeInBytes: 16);
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

            var payload = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, payload, nonce.Length + tag.Length, ciphertext.Length);

            return Convert.ToBase64String(payload);
        }

        private static string GenerateLuhnValidNumber()
        {
            const int length = 16;
            const string issuerPrefix = "4000";

            var digits = new int[length];
            for (var i = 0; i < issuerPrefix.Length; i++)
                digits[i] = issuerPrefix[i] - '0';

            for (var i = issuerPrefix.Length; i < length - 1; i++)
                digits[i] = RandomNumberGenerator.GetInt32(0, 10);

            digits[length - 1] = CalculateLuhnCheckDigit(digits, length - 1);
            return string.Concat(digits);
        }

        private static int CalculateLuhnCheckDigit(int[] digits, int checkDigitIndex)
        {
            var sum = 0;
            var doubleDigit = true;

            for (var i = checkDigitIndex - 1; i >= 0; i--)
            {
                var digit = digits[i];
                if (doubleDigit)
                {
                    digit *= 2;
                    if (digit > 9) digit -= 9;
                }
                sum += digit;
                doubleDigit = !doubleDigit;
            }

            return (10 - (sum % 10)) % 10;
        }
    }
}