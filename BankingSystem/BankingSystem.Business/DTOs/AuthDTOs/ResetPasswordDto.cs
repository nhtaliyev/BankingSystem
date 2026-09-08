namespace BankingSystem.Business.DTOs.AuthDTOs
{
    public record ResetPasswordDto(string Email, string Token, string NewPassword, string ConfirmNewPassword);
}
