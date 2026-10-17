using System.ComponentModel.DataAnnotations;

namespace BankingSystem.Business.ExternalServices
{
    public class CardEncryptionOptions
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "CardEncryption:HashKey is required.")]
        public string HashKey { get; set; } = null!;

        [Required(AllowEmptyStrings = false, ErrorMessage = "CardEncryption:EncryptionKey is required.")]
        public string EncryptionKey { get; set; } = null!;
    }
}