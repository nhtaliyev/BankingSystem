namespace BankingSystem.MVC.ViewModels
{
    public record LoginRequest(string Email, string Password, bool RememberMe);

    public record RegisterRequest(
        string FullName, string Email, string Password, string ConfirmPassword,
        string PhoneNumber, DateTime Birthday);

    public record RefreshRequest(string AccessToken, string RefreshToken);

    public record TokenResponse(string AccessToken, DateTime ExpireDate, string RefreshToken);
}
