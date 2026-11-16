using System.ComponentModel.DataAnnotations;

namespace BankingSystem.MVC.ViewModels
{
    public class RegisterViewModel
    {
        [Required, StringLength(100)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\+?[1-9]\d{7,14}$", ErrorMessage = "Phone number must be in international format.")]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required, DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }

        [Required, StringLength(64, MinimumLength = 10)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Password and confirm password don't match")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
