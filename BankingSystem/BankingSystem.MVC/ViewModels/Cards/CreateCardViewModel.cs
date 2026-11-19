using System.ComponentModel.DataAnnotations;

namespace BankingSystem.MVC.ViewModels.Cards
{
    public class CreateCardViewModel
    {
        public int AccountId { get; set; }

        [Required(ErrorMessage = "Please enter a daily limit")]
        [Range(0.01, 10000, ErrorMessage = "Daily limit must be between 0.01 and 10,000")]
        [Display(Name = "Daily limit")]
        public decimal? DailyLimit { get; set; }
    }
}
