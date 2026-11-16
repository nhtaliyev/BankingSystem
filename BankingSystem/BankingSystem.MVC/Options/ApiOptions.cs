using System.ComponentModel.DataAnnotations;

namespace BankingSystem.MVC.Options;

public class ApiOptions
{
    public const string SectionName = "Api";

    [Required, Url]
    public string BaseUrl { get; set; } = string.Empty;
}