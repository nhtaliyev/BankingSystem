using BankingSystem.MVC.Enums;
using System.ComponentModel.DataAnnotations;

namespace BankingSystem.MVC.ViewModels.Accounts
{
    public class CreateAccountViewModel
    {
        [Required(ErrorMessage = "Please choose a currency")]
        public Currency? Currency { get; set; }
    }
}
