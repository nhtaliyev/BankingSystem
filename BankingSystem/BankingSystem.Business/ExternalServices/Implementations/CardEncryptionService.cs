using BankingSystem.Business.ExternalServices.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace BankingSystem.Business.ExternalServices.Implementations
{
    public class CardEncryptionService : ICardEncryptionService
    {
        // Payload layout (then Base64 encoded):
        // [ 1 byte key version ][ 12 bytes nonce ][ 16 bytes GCM tag ][ ciphertext ]
        // The version byte is also passed to AES-GCM as associated data, so it cannot be tampered with.
        private const byte CurrentKeyVersion = 1;
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private const int HeaderSize = 1 + NonceSize + TagSize;

        private const int MinHashKeySize = 32;
        private const int EncryptionKeySize = 32; // AES-256

        private const string IssuerPrefix = "9999";
        private const int CardNumberLength = 16;

        private readonly byte[] _hashKey;
        private readonly byte[] _encryptionKey;

        public CardEncryptionService(IOptions<CardEncryptionOptions> options)
        {
            _hashKey = DecodeKey(options.Value.HashKey, nameof(CardEncryptionOptions.HashKey));
            _encryptionKey = DecodeKey(options.Value.EncryptionKey, nameof(CardEncryptionOptions.EncryptionKey));

            if (_hashKey.Length < MinHashKeySize)
                throw new InvalidOperationException($"HashKey must be at least {MinHashKeySize} bytes.");

            if (_encryptionKey.Length != EncryptionKeySize)
                throw new InvalidOperationException($"EncryptionKey must be exactly {EncryptionKeySize} bytes (AES-256).");

            if (CryptographicOperations.FixedTimeEquals(_hashKey, _encryptionKey))
                throw new InvalidOperationException("HashKey and EncryptionKey must be two different secrets.");
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
            return Convert.ToHexString(ComputeHashBytes(cardNumber));
        }

        public bool VerifyCardNumber(string cardNumber, string storedHash)
        {
            if (string.IsNullOrEmpty(cardNumber) || string.IsNullOrEmpty(storedHash))
                return false;

            byte[] storedBytes;
            try
            {
                storedBytes = Convert.FromHexString(storedHash);
            }
            catch (FormatException)
            {
                return false;
            }

            var computedBytes = ComputeHashBytes(cardNumber);

            return CryptographicOperations.FixedTimeEquals(computedBytes, storedBytes);
        }

        public string Decrypt(string encrypted)
        {
            if (string.IsNullOrEmpty(encrypted))
                throw new CryptographicException("Encrypted card data is empty.");

            byte[] payload;
            try
            {
                payload = Convert.FromBase64String(encrypted);
            }
            catch (FormatException)
            {
                throw new CryptographicException("Encrypted card data is not valid Base64.");
            }

            if (payload.Length <= HeaderSize)
                throw new CryptographicException("Encrypted card data is too short.");

            if (payload[0] != CurrentKeyVersion)
                throw new CryptographicException($"Unsupported key version {payload[0]}.");

            var associatedData = payload.AsSpan(0, 1);
            var nonce = payload.AsSpan(1, NonceSize);
            var tag = payload.AsSpan(1 + NonceSize, TagSize);
            var ciphertext = payload.AsSpan(HeaderSize);
            var plaintext = new byte[ciphertext.Length];

            try
            {
                using var aesGcm = new AesGcm(_encryptionKey, TagSize);
                aesGcm.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);

                return Encoding.UTF8.GetString(plaintext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        }

        private string Encrypt(string plainCardNumber)
        {
            var plaintext = Encoding.UTF8.GetBytes(plainCardNumber);
            var payload = new byte[HeaderSize + plaintext.Length];

            payload[0] = CurrentKeyVersion;

            var associatedData = payload.AsSpan(0, 1);
            var nonce = payload.AsSpan(1, NonceSize);
            var tag = payload.AsSpan(1 + NonceSize, TagSize);
            var ciphertext = payload.AsSpan(HeaderSize);

            RandomNumberGenerator.Fill(nonce);

            try
            {
                using var aesGcm = new AesGcm(_encryptionKey, TagSize);
                aesGcm.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }

            return Convert.ToBase64String(payload);
        }

        private byte[] ComputeHashBytes(string cardNumber)
        {
            var data = Encoding.UTF8.GetBytes(cardNumber);
            try
            {
                return HMACSHA256.HashData(_hashKey, data);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(data);
            }
        }

        private static byte[] DecodeKey(string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"{name} is not configured.");

            try
            {
                return Convert.FromBase64String(value);
            }
            catch (FormatException)
            {
                // Deliberately do not include the value in the message.
                throw new InvalidOperationException($"{name} is not valid Base64.");
            }
        }

        private static string GenerateLuhnValidNumber()
        {
            var digits = new int[CardNumberLength];
            for (var i = 0; i < IssuerPrefix.Length; i++)
                digits[i] = IssuerPrefix[i] - '0';

            for (var i = IssuerPrefix.Length; i < CardNumberLength - 1; i++)
                digits[i] = RandomNumberGenerator.GetInt32(0, 10);

            digits[CardNumberLength - 1] = CalculateLuhnCheckDigit(digits, CardNumberLength - 1);
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