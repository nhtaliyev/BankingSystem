using System.ComponentModel.DataAnnotations;

namespace BankingSystem.MVC.ViewModels
{
    public class LoginViewModel
    {
        [Required, EmailAddress, StringLength(60)]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
